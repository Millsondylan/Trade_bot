using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using TradingBot.Framework.RiskManagement;
using TradingBot.Framework.Logging;
using TradingBot.Framework.ErrorHandling;

namespace TradingBot.Strategies
{
    /// <summary>
    /// RSI + Bollinger Bands Mean Reversion Strategy with Trend Filter
    ///
    /// STRATEGY OVERVIEW:
    /// Conservative mean reversion approach targeting 15-30% annual returns with 10-20% max drawdown.
    /// Works best in ranging markets on major forex pairs during high liquidity sessions.
    ///
    /// ENTRY RULES:
    /// - LONG: Price touches lower Bollinger Band + RSI < 30 + Price above 200 EMA (uptrend filter)
    /// - SHORT: Price touches upper Bollinger Band + RSI > 70 + Price below 200 EMA (downtrend filter)
    ///
    /// EXIT RULES:
    /// - Take Profit: Middle Bollinger Band or opposite band
    /// - Stop Loss: 50 pips or 2x ATR (dynamic)
    ///
    /// BEST PAIRS: EUR/USD, USD/CAD, EUR/GBP
    /// TIMEFRAME: 1-hour
    /// EXPECTED WIN RATE: 55-65%
    /// RISK-REWARD: 1:1.5 to 1:2
    /// </summary>
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class RSI_BollingerBands_MeanReversion : Robot
    {
        #region Parameters

        [Parameter("Risk Per Trade (%)", DefaultValue = 1.0, MinValue = 0.1, MaxValue = 5.0)]
        public double RiskPercent { get; set; }

        [Parameter("RSI Period", DefaultValue = 2, MinValue = 2, MaxValue = 14)]
        public int RsiPeriod { get; set; }

        [Parameter("RSI Oversold", DefaultValue = 30, MinValue = 10, MaxValue = 40)]
        public double RsiOversold { get; set; }

        [Parameter("RSI Overbought", DefaultValue = 70, MinValue = 60, MaxValue = 90)]
        public double RsiOverbought { get; set; }

        [Parameter("BB Period", DefaultValue = 20, MinValue = 10, MaxValue = 50)]
        public int BbPeriod { get; set; }

        [Parameter("BB Std Dev", DefaultValue = 2.0, MinValue = 1.0, MaxValue = 3.0)]
        public double BbStdDev { get; set; }

        [Parameter("Trend EMA Period", DefaultValue = 200, MinValue = 50, MaxValue = 300)]
        public int TrendEmaPeriod { get; set; }

        [Parameter("ATR Period", DefaultValue = 14, MinValue = 7, MaxValue = 30)]
        public int AtrPeriod { get; set; }

        [Parameter("ATR Stop Loss Multiplier", DefaultValue = 2.0, MinValue = 1.0, MaxValue = 5.0)]
        public double AtrStopMultiplier { get; set; }

        [Parameter("Fixed Stop Loss (pips)", DefaultValue = 50, MinValue = 10, MaxValue = 200)]
        public double FixedStopLoss { get; set; }

        [Parameter("Use ATR for Stop Loss", DefaultValue = true)]
        public bool UseAtrStopLoss { get; set; }

        [Parameter("Max Daily Loss %", DefaultValue = 2.0, MinValue = 1.0, MaxValue = 10.0)]
        public double MaxDailyLossPercent { get; set; }

        [Parameter("Max Weekly Loss %", DefaultValue = 5.0, MinValue = 2.0, MaxValue = 20.0)]
        public double MaxWeeklyLossPercent { get; set; }

        [Parameter("Max Drawdown %", DefaultValue = 20.0, MinValue = 10.0, MaxValue = 50.0)]
        public double MaxDrawdownPercent { get; set; }

        [Parameter("Max Concurrent Positions", DefaultValue = 5, MinValue = 1, MaxValue = 20)]
        public int MaxConcurrentPositions { get; set; }

        #endregion

        #region Private Fields

        private RiskManager _riskManager;
        private PositionSizer _positionSizer;
        private TradingLogger _logger;
        private ErrorHandler _errorHandler;

        private RelativeStrengthIndex _rsi;
        private BollingerBands _bollingerBands;
        private ExponentialMovingAverage _trendEma;
        private AverageTrueRange _atr;

        private const string STRATEGY_NAME = "RSI_BB_MeanReversion";
        private const string POSITION_LABEL = "RSIBR_MR";

        #endregion

        protected override void OnStart()
        {
            Print("========================================");
            Print($"  {STRATEGY_NAME} Starting");
            Print("========================================");
            Print($"Symbol: {SymbolName}");
            Print($"Timeframe: {TimeFrame}");
            Print($"Risk Per Trade: {RiskPercent}%");
            Print($"Initial Balance: {Account.Balance:F2}");
            Print("========================================\n");

            // Initialize framework components
            _riskManager = new RiskManager(
                this,
                MaxDailyLossPercent,
                MaxWeeklyLossPercent,
                MaxDrawdownPercent,
                MaxConcurrentPositions
            );

            _positionSizer = new PositionSizer(this, Symbol);
            _logger = new TradingLogger(this);
            _errorHandler = new ErrorHandler(this, maxRetries: 3);

            // Initialize indicators
            _rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, RsiPeriod);
            _bollingerBands = Indicators.BollingerBands(Bars.ClosePrices, BbPeriod, BbStdDev, MovingAverageType.Simple);
            _trendEma = Indicators.ExponentialMovingAverage(Bars.ClosePrices, TrendEmaPeriod);
            _atr = Indicators.AverageTrueRange(AtrPeriod, MovingAverageType.Simple);

            Print("Indicators initialized successfully");
            Print($"RSI Period: {RsiPeriod}, Oversold: {RsiOversold}, Overbought: {RsiOverbought}");
            Print($"Bollinger Bands: {BbPeriod} period, {BbStdDev} std dev");
            Print($"Trend Filter: {TrendEmaPeriod} EMA");
            Print($"ATR: {AtrPeriod} period, Stop Loss: {AtrStopMultiplier}x ATR\n");

            // Subscribe to position events
            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;
        }

        protected override void OnBar()
        {
            try
            {
                // Check risk limits first
                if (!_riskManager.CheckRiskLimits())
                {
                    return;
                }

                // Check circuit breaker
                if (_errorHandler.ShouldTripCircuitBreaker())
                {
                    Stop();
                    return;
                }

                // Get current values
                double rsiValue = _rsi.Result.LastValue;
                double currentClose = Bars.ClosePrices.LastValue;
                double upperBand = _bollingerBands.Top.LastValue;
                double middleBand = _bollingerBands.Main.LastValue;
                double lowerBand = _bollingerBands.Bottom.LastValue;
                double trendEma = _trendEma.Result.LastValue;
                double atrValue = _atr.Result.LastValue;

                // Log current state periodically
                if (Bars.Count % 10 == 0)
                {
                    _logger.LogDebug($"RSI: {rsiValue:F2} | Price: {currentClose:F5} | BB: [{lowerBand:F5}, {middleBand:F5}, {upperBand:F5}] | EMA200: {trendEma:F5}");
                }

                // Don't trade if we already have open positions for this strategy
                var existingPositions = Positions.FindAll(POSITION_LABEL, SymbolName);
                if (existingPositions.Length > 0)
                {
                    ManageOpenPositions(existingPositions);
                    return;
                }

                // Check for LONG entry signal
                if (CheckLongEntry(rsiValue, currentClose, lowerBand, trendEma))
                {
                    ExecuteLongEntry(currentClose, atrValue);
                }
                // Check for SHORT entry signal
                else if (CheckShortEntry(rsiValue, currentClose, upperBand, trendEma))
                {
                    ExecuteShortEntry(currentClose, atrValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in OnBar: {ex.Message}", ex);
            }
        }

        protected override void OnStop()
        {
            Print("\n========================================");
            Print($"  {STRATEGY_NAME} Stopping");
            Print("========================================");

            // Print final performance report
            _logger.PrintPerformanceReport();

            // Export trade logs
            string csvData = _logger.ExportToCSV();
            Print("\nTrade Log CSV (copy for external analysis):");
            Print(csvData);

            Print("========================================\n");
        }

        #region Entry Logic

        private bool CheckLongEntry(double rsiValue, double currentClose, double lowerBand, double trendEma)
        {
            // LONG conditions:
            // 1. Price touches or is below lower Bollinger Band
            // 2. RSI is oversold (< 30)
            // 3. Price is above 200 EMA (uptrend filter)

            bool priceTouchesLowerBand = currentClose <= lowerBand * 1.001; // Allow 0.1% tolerance
            bool rsiOversold = rsiValue < RsiOversold;
            bool inUptrend = currentClose > trendEma;

            return priceTouchesLowerBand && rsiOversold && inUptrend;
        }

        private bool CheckShortEntry(double rsiValue, double currentClose, double upperBand, double trendEma)
        {
            // SHORT conditions:
            // 1. Price touches or is above upper Bollinger Band
            // 2. RSI is overbought (> 70)
            // 3. Price is below 200 EMA (downtrend filter)

            bool priceTouchesUpperBand = currentClose >= upperBand * 0.999; // Allow 0.1% tolerance
            bool rsiOverbought = rsiValue > RsiOverbought;
            bool inDowntrend = currentClose < trendEma;

            return priceTouchesUpperBand && rsiOverbought && inDowntrend;
        }

        private void ExecuteLongEntry(double entryPrice, double atrValue)
        {
            if (!_riskManager.CanOpenNewPosition())
            {
                _logger.LogDebug("Cannot open LONG: Risk limits or position limits reached");
                return;
            }

            // Calculate stop loss
            double stopLossPrice;
            double stopLossPips;

            if (UseAtrStopLoss)
            {
                stopLossPrice = entryPrice - (atrValue * AtrStopMultiplier);
                stopLossPips = _positionSizer.CalculateStopLossPips(entryPrice, stopLossPrice);
            }
            else
            {
                stopLossPips = FixedStopLoss;
                stopLossPrice = entryPrice - (stopLossPips * Symbol.PipSize);
            }

            // Calculate position size
            double positionSize = _positionSizer.CalculatePositionSizeByRisk(RiskPercent, stopLossPips);

            if (!_positionSizer.ValidatePositionSize(positionSize))
            {
                _logger.LogError("Invalid position size calculated for LONG entry");
                return;
            }

            // Calculate take profit (middle Bollinger Band)
            double takeProfitPrice = _bollingerBands.Main.LastValue;

            // Validate parameters
            if (!_errorHandler.ValidateOrderParameters(Symbol, positionSize, stopLossPrice, takeProfitPrice, TradeType.Buy))
            {
                _logger.LogError("Order parameters validation failed for LONG entry");
                return;
            }

            // Execute trade with error handling
            var result = _errorHandler.ExecuteWithRetry(
                () => ExecuteMarketOrder(TradeType.Buy, SymbolName, positionSize, POSITION_LABEL, stopLossPips, null),
                "LONG entry"
            );

            if (!_errorHandler.HandleTradeResult(result, "LONG entry"))
            {
                _logger.LogError($"Failed to execute LONG entry: {result.Error}");
                return;
            }

            // Modify take profit
            var position = result.Position;
            position.ModifyTakeProfitPrice(takeProfitPrice);

            string signalDetails = $"RSI={_rsi.Result.LastValue:F2}, Price={entryPrice:F5}, LowerBB={_bollingerBands.Bottom.LastValue:F5}, EMA200={_trendEma.Result.LastValue:F5}";
            _logger.LogTradeEntry(position, STRATEGY_NAME, signalDetails);
        }

        private void ExecuteShortEntry(double entryPrice, double atrValue)
        {
            if (!_riskManager.CanOpenNewPosition())
            {
                _logger.LogDebug("Cannot open SHORT: Risk limits or position limits reached");
                return;
            }

            // Calculate stop loss
            double stopLossPrice;
            double stopLossPips;

            if (UseAtrStopLoss)
            {
                stopLossPrice = entryPrice + (atrValue * AtrStopMultiplier);
                stopLossPips = _positionSizer.CalculateStopLossPips(entryPrice, stopLossPrice);
            }
            else
            {
                stopLossPips = FixedStopLoss;
                stopLossPrice = entryPrice + (stopLossPips * Symbol.PipSize);
            }

            // Calculate position size
            double positionSize = _positionSizer.CalculatePositionSizeByRisk(RiskPercent, stopLossPips);

            if (!_positionSizer.ValidatePositionSize(positionSize))
            {
                _logger.LogError("Invalid position size calculated for SHORT entry");
                return;
            }

            // Calculate take profit (middle Bollinger Band)
            double takeProfitPrice = _bollingerBands.Main.LastValue;

            // Validate parameters
            if (!_errorHandler.ValidateOrderParameters(Symbol, positionSize, stopLossPrice, takeProfitPrice, TradeType.Sell))
            {
                _logger.LogError("Order parameters validation failed for SHORT entry");
                return;
            }

            // Execute trade with error handling
            var result = _errorHandler.ExecuteWithRetry(
                () => ExecuteMarketOrder(TradeType.Sell, SymbolName, positionSize, POSITION_LABEL, stopLossPips, null),
                "SHORT entry"
            );

            if (!_errorHandler.HandleTradeResult(result, "SHORT entry"))
            {
                _logger.LogError($"Failed to execute SHORT entry: {result.Error}");
                return;
            }

            // Modify take profit
            var position = result.Position;
            position.ModifyTakeProfitPrice(takeProfitPrice);

            string signalDetails = $"RSI={_rsi.Result.LastValue:F2}, Price={entryPrice:F5}, UpperBB={_bollingerBands.Top.LastValue:F5}, EMA200={_trendEma.Result.LastValue:F5}";
            _logger.LogTradeEntry(position, STRATEGY_NAME, signalDetails);
        }

        #endregion

        #region Position Management

        private void ManageOpenPositions(Position[] positions)
        {
            foreach (var position in positions)
            {
                // Update take profit to current middle band if price has moved favorably
                double middleBand = _bollingerBands.Main.LastValue;
                double currentPrice = position.TradeType == TradeType.Buy ? Symbol.Bid : Symbol.Ask;

                // For longs, if price is above entry and middle band is higher than current TP
                if (position.TradeType == TradeType.Buy && currentPrice > position.EntryPrice)
                {
                    if (position.TakeProfit.HasValue && middleBand > position.TakeProfit.Value)
                    {
                        _errorHandler.ModifyPositionSafely(position, position.StopLoss, middleBand);
                        _logger.LogDebug($"Updated LONG TP to middle band: {middleBand:F5}");
                    }
                }
                // For shorts, if price is below entry and middle band is lower than current TP
                else if (position.TradeType == TradeType.Sell && currentPrice < position.EntryPrice)
                {
                    if (position.TakeProfit.HasValue && middleBand < position.TakeProfit.Value)
                    {
                        _errorHandler.ModifyPositionSafely(position, position.StopLoss, middleBand);
                        _logger.LogDebug($"Updated SHORT TP to middle band: {middleBand:F5}");
                    }
                }
            }
        }

        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            var position = args.Position;
            if (position.Label == POSITION_LABEL)
            {
                Print($"\n[POSITION OPENED] {position.TradeType} {position.VolumeInUnits} units @ {position.EntryPrice:F5}");
                Print($"  SL: {position.StopLoss:F5} | TP: {position.TakeProfit:F5}");
                Print($"  Position ID: {position.Id}\n");
            }
        }

        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            var position = args.Position;
            if (position.Label == POSITION_LABEL)
            {
                string exitReason = position.NetProfit > 0 ? "Take Profit Hit" :
                                  position.NetProfit < 0 ? "Stop Loss Hit" : "Break Even";

                _logger.LogTradeExit(position, exitReason);

                Print($"\n[POSITION CLOSED] {position.TradeType} {position.SymbolName}");
                Print($"  P/L: {position.NetProfit:F2} ({position.Pips:F1} pips)");
                Print($"  Reason: {exitReason}\n");

                // Print risk statistics
                var riskStats = _riskManager.GetRiskStatistics();
                Print($"[RISK STATS] Daily Loss: {riskStats.DailyLossPercent:F2}% | Drawdown: {riskStats.DrawdownPercent:F2}%\n");
            }
        }

        #endregion
    }
}
