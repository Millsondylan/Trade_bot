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
    /// EMA Trend Following Strategy with ADX Confirmation
    ///
    /// STRATEGY OVERVIEW:
    /// Moderate risk trend following approach targeting 20-40% annual returns with 15-25% max drawdown.
    /// Works best during strong directional moves on major forex pairs.
    ///
    /// ENTRY RULES:
    /// - LONG: Fast EMA crosses above Slow EMA + ADX > 25 + Daily trend is up (multi-timeframe)
    /// - SHORT: Fast EMA crosses below Slow EMA + ADX > 25 + Daily trend is down
    ///
    /// EXIT RULES:
    /// - Take Profit: 2x risk (1:2 risk-reward minimum)
    /// - Stop Loss: Below recent swing low (for longs) or above swing high (for shorts)
    /// - Trailing stop: Move SL to break-even when 1R in profit
    ///
    /// BEST PAIRS: GBP/USD, EUR/USD, AUD/USD
    /// TIMEFRAME: 1-hour (with daily confirmation)
    /// EXPECTED WIN RATE: 40-50%
    /// RISK-REWARD: 1:2 to 1:3
    /// </summary>
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class EMA_Trend_Following_ADX : Robot
    {
        #region Parameters

        [Parameter("Risk Per Trade (%)", DefaultValue = 1.5, MinValue = 0.5, MaxValue = 5.0)]
        public double RiskPercent { get; set; }

        [Parameter("Fast EMA Period", DefaultValue = 21, MinValue = 10, MaxValue = 50)]
        public int FastEmaPeriod { get; set; }

        [Parameter("Slow EMA Period", DefaultValue = 50, MinValue = 30, MaxValue = 100)]
        public int SlowEmaPeriod { get; set; }

        [Parameter("Trend EMA Period", DefaultValue = 200, MinValue = 100, MaxValue = 300)]
        public int TrendEmaPeriod { get; set; }

        [Parameter("ADX Period", DefaultValue = 14, MinValue = 10, MaxValue = 30)]
        public int AdxPeriod { get; set; }

        [Parameter("ADX Minimum", DefaultValue = 25, MinValue = 15, MaxValue = 40)]
        public double AdxMinimum { get; set; }

        [Parameter("Risk-Reward Ratio", DefaultValue = 2.0, MinValue = 1.5, MaxValue = 5.0)]
        public double RiskRewardRatio { get; set; }

        [Parameter("Use Trailing Stop", DefaultValue = true)]
        public bool UseTrailingStop { get; set; }

        [Parameter("Breakeven Trigger (R)", DefaultValue = 1.0, MinValue = 0.5, MaxValue = 2.0)]
        public double BreakevenTrigger { get; set; }

        [Parameter("Swing High/Low Lookback", DefaultValue = 20, MinValue = 10, MaxValue = 50)]
        public int SwingLookback { get; set; }

        [Parameter("Max Daily Loss %", DefaultValue = 3.0, MinValue = 1.0, MaxValue = 10.0)]
        public double MaxDailyLossPercent { get; set; }

        [Parameter("Max Weekly Loss %", DefaultValue = 7.0, MinValue = 2.0, MaxValue = 20.0)]
        public double MaxWeeklyLossPercent { get; set; }

        [Parameter("Max Drawdown %", DefaultValue = 25.0, MinValue = 15.0, MaxValue = 50.0)]
        public double MaxDrawdownPercent { get; set; }

        [Parameter("Max Concurrent Positions", DefaultValue = 3, MinValue = 1, MaxValue = 10)]
        public int MaxConcurrentPositions { get; set; }

        #endregion

        #region Private Fields

        private RiskManager _riskManager;
        private PositionSizer _positionSizer;
        private TradingLogger _logger;
        private ErrorHandler _errorHandler;

        private ExponentialMovingAverage _fastEma;
        private ExponentialMovingAverage _slowEma;
        private ExponentialMovingAverage _trendEma;
        private DirectionalMovementSystem _adx;

        // Multi-timeframe data
        private Bars _dailyBars;
        private ExponentialMovingAverage _dailyFastEma;
        private ExponentialMovingAverage _dailySlowEma;

        private bool _lastCrossWasBullish;
        private int _lastCrossBarIndex;

        private const string STRATEGY_NAME = "EMA_Trend_ADX";
        private const string POSITION_LABEL = "EMA_TREND";

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

            // Initialize current timeframe indicators
            _fastEma = Indicators.ExponentialMovingAverage(Bars.ClosePrices, FastEmaPeriod);
            _slowEma = Indicators.ExponentialMovingAverage(Bars.ClosePrices, SlowEmaPeriod);
            _trendEma = Indicators.ExponentialMovingAverage(Bars.ClosePrices, TrendEmaPeriod);
            _adx = Indicators.DirectionalMovementSystem(AdxPeriod);

            // Initialize multi-timeframe (daily) data
            _dailyBars = MarketData.GetBars(TimeFrame.Daily);
            _dailyFastEma = Indicators.ExponentialMovingAverage(_dailyBars.ClosePrices, FastEmaPeriod);
            _dailySlowEma = Indicators.ExponentialMovingAverage(_dailyBars.ClosePrices, SlowEmaPeriod);

            _lastCrossBarIndex = -1;

            Print("Indicators initialized successfully");
            Print($"Current TF EMAs: Fast={FastEmaPeriod}, Slow={SlowEmaPeriod}, Trend={TrendEmaPeriod}");
            Print($"Daily TF EMAs: Fast={FastEmaPeriod}, Slow={SlowEmaPeriod}");
            Print($"ADX: {AdxPeriod} period, Minimum: {AdxMinimum}");
            Print($"Risk-Reward Ratio: 1:{RiskRewardRatio}");
            Print($"Trailing Stop: {(UseTrailingStop ? "Enabled" : "Disabled")}\n");

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
                int currentIndex = Bars.Count - 1;
                double fastEmaValue = _fastEma.Result.LastValue;
                double slowEmaValue = _slowEma.Result.LastValue;
                double trendEmaValue = _trendEma.Result.LastValue;
                double adxValue = _adx.ADX.LastValue;
                double currentClose = Bars.ClosePrices.LastValue;

                // Get daily timeframe values
                int dailyIndex = _dailyBars.OpenTimes.GetIndexByTime(Bars.LastBar.OpenTime);
                double dailyFastEma = _dailyFastEma.Result[dailyIndex];
                double dailySlowEma = _dailySlowEma.Result[dailyIndex];

                // Log current state periodically
                if (Bars.Count % 10 == 0)
                {
                    _logger.LogDebug($"Fast EMA: {fastEmaValue:F5} | Slow EMA: {slowEmaValue:F5} | ADX: {adxValue:F2} | Daily Trend: {(dailyFastEma > dailySlowEma ? "UP" : "DOWN")}");
                }

                // Manage existing positions (trailing stops, etc.)
                var existingPositions = Positions.FindAll(POSITION_LABEL, SymbolName);
                if (existingPositions.Length > 0)
                {
                    ManageOpenPositions(existingPositions);

                    // Don't open new positions if we're at max
                    if (existingPositions.Length >= MaxConcurrentPositions)
                    {
                        return;
                    }
                }

                // Detect EMA crossover
                double prevFastEma = _fastEma.Result.Last(1);
                double prevSlowEma = _slowEma.Result.Last(1);

                bool bullishCross = prevFastEma <= prevSlowEma && fastEmaValue > slowEmaValue;
                bool bearishCross = prevFastEma >= prevSlowEma && fastEmaValue < slowEmaValue;

                // Avoid multiple signals on same cross
                if (bullishCross || bearishCross)
                {
                    if (currentIndex == _lastCrossBarIndex)
                    {
                        return; // Already processed this cross
                    }

                    _lastCrossBarIndex = currentIndex;
                    _lastCrossWasBullish = bullishCross;
                }
                else
                {
                    return; // No cross detected
                }

                // Check for LONG entry signal
                if (bullishCross && CheckLongEntry(adxValue, currentClose, trendEmaValue, dailyFastEma, dailySlowEma))
                {
                    ExecuteLongEntry();
                }
                // Check for SHORT entry signal
                else if (bearishCross && CheckShortEntry(adxValue, currentClose, trendEmaValue, dailyFastEma, dailySlowEma))
                {
                    ExecuteShortEntry();
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

        private bool CheckLongEntry(double adxValue, double currentClose, double trendEmaValue, double dailyFastEma, double dailySlowEma)
        {
            // LONG conditions:
            // 1. Fast EMA crossed above Slow EMA (already checked)
            // 2. ADX > minimum (strong trend)
            // 3. Price above 200 EMA (long-term uptrend)
            // 4. Daily timeframe confirms uptrend (Fast EMA > Slow EMA)

            bool strongTrend = adxValue > AdxMinimum;
            bool aboveTrendFilter = currentClose > trendEmaValue;
            bool dailyTrendUp = dailyFastEma > dailySlowEma;

            bool longSignal = strongTrend && aboveTrendFilter && dailyTrendUp;

            if (longSignal)
            {
                Print($"\n[LONG SIGNAL] ADX={adxValue:F2}, Price={currentClose:F5}, TrendEMA={trendEmaValue:F5}, Daily={dailyFastEma:F5}>{dailySlowEma:F5}");
            }

            return longSignal;
        }

        private bool CheckShortEntry(double adxValue, double currentClose, double trendEmaValue, double dailyFastEma, double dailySlowEma)
        {
            // SHORT conditions:
            // 1. Fast EMA crossed below Slow EMA (already checked)
            // 2. ADX > minimum (strong trend)
            // 3. Price below 200 EMA (long-term downtrend)
            // 4. Daily timeframe confirms downtrend (Fast EMA < Slow EMA)

            bool strongTrend = adxValue > AdxMinimum;
            bool belowTrendFilter = currentClose < trendEmaValue;
            bool dailyTrendDown = dailyFastEma < dailySlowEma;

            bool shortSignal = strongTrend && belowTrendFilter && dailyTrendDown;

            if (shortSignal)
            {
                Print($"\n[SHORT SIGNAL] ADX={adxValue:F2}, Price={currentClose:F5}, TrendEMA={trendEmaValue:F5}, Daily={dailyFastEma:F5}<{dailySlowEma:F5}");
            }

            return shortSignal;
        }

        private void ExecuteLongEntry()
        {
            if (!_riskManager.CanOpenNewPosition())
            {
                _logger.LogDebug("Cannot open LONG: Risk limits or position limits reached");
                return;
            }

            double entryPrice = Symbol.Ask;

            // Find swing low for stop loss
            double swingLow = FindSwingLow(SwingLookback);
            double stopLossPrice = swingLow - (5 * Symbol.PipSize); // 5 pips buffer
            double stopLossPips = _positionSizer.CalculateStopLossPips(entryPrice, stopLossPrice);

            // Limit stop loss to reasonable size (max 100 pips)
            if (stopLossPips > 100)
            {
                _logger.LogWarning($"Stop loss too large: {stopLossPips:F1} pips, limiting to 100 pips");
                stopLossPips = 100;
                stopLossPrice = entryPrice - (stopLossPips * Symbol.PipSize);
            }

            // Calculate position size
            double positionSize = _positionSizer.CalculatePositionSizeByRisk(RiskPercent, stopLossPips);

            if (!_positionSizer.ValidatePositionSize(positionSize))
            {
                _logger.LogError("Invalid position size calculated for LONG entry");
                return;
            }

            // Calculate take profit based on risk-reward ratio
            double takeProfitPrice = _positionSizer.CalculateTakeProfitPrice(entryPrice, stopLossPrice, RiskRewardRatio, TradeType.Buy);

            // Validate parameters
            if (!_errorHandler.ValidateOrderParameters(Symbol, positionSize, stopLossPrice, takeProfitPrice, TradeType.Buy))
            {
                _logger.LogError("Order parameters validation failed for LONG entry");
                return;
            }

            // Execute trade with error handling
            var result = _errorHandler.ExecuteWithRetry(
                () => ExecuteMarketOrder(TradeType.Buy, SymbolName, positionSize, POSITION_LABEL, stopLossPips, (takeProfitPrice - entryPrice) / Symbol.PipSize),
                "LONG entry"
            );

            if (!_errorHandler.HandleTradeResult(result, "LONG entry"))
            {
                _logger.LogError($"Failed to execute LONG entry: {result.Error}");
                return;
            }

            string signalDetails = $"FastEMA={_fastEma.Result.LastValue:F5} > SlowEMA={_slowEma.Result.LastValue:F5}, ADX={_adx.ADX.LastValue:F2}, R:R=1:{RiskRewardRatio}";
            _logger.LogTradeEntry(result.Position, STRATEGY_NAME, signalDetails);
        }

        private void ExecuteShortEntry()
        {
            if (!_riskManager.CanOpenNewPosition())
            {
                _logger.LogDebug("Cannot open SHORT: Risk limits or position limits reached");
                return;
            }

            double entryPrice = Symbol.Bid;

            // Find swing high for stop loss
            double swingHigh = FindSwingHigh(SwingLookback);
            double stopLossPrice = swingHigh + (5 * Symbol.PipSize); // 5 pips buffer
            double stopLossPips = _positionSizer.CalculateStopLossPips(entryPrice, stopLossPrice);

            // Limit stop loss to reasonable size (max 100 pips)
            if (stopLossPips > 100)
            {
                _logger.LogWarning($"Stop loss too large: {stopLossPips:F1} pips, limiting to 100 pips");
                stopLossPips = 100;
                stopLossPrice = entryPrice + (stopLossPips * Symbol.PipSize);
            }

            // Calculate position size
            double positionSize = _positionSizer.CalculatePositionSizeByRisk(RiskPercent, stopLossPips);

            if (!_positionSizer.ValidatePositionSize(positionSize))
            {
                _logger.LogError("Invalid position size calculated for SHORT entry");
                return;
            }

            // Calculate take profit based on risk-reward ratio
            double takeProfitPrice = _positionSizer.CalculateTakeProfitPrice(entryPrice, stopLossPrice, RiskRewardRatio, TradeType.Sell);

            // Validate parameters
            if (!_errorHandler.ValidateOrderParameters(Symbol, positionSize, stopLossPrice, takeProfitPrice, TradeType.Sell))
            {
                _logger.LogError("Order parameters validation failed for SHORT entry");
                return;
            }

            // Execute trade with error handling
            var result = _errorHandler.ExecuteWithRetry(
                () => ExecuteMarketOrder(TradeType.Sell, SymbolName, positionSize, POSITION_LABEL, stopLossPips, (entryPrice - takeProfitPrice) / Symbol.PipSize),
                "SHORT entry"
            );

            if (!_errorHandler.HandleTradeResult(result, "SHORT entry"))
            {
                _logger.LogError($"Failed to execute SHORT entry: {result.Error}");
                return;
            }

            string signalDetails = $"FastEMA={_fastEma.Result.LastValue:F5} < SlowEMA={_slowEma.Result.LastValue:F5}, ADX={_adx.ADX.LastValue:F2}, R:R=1:{RiskRewardRatio}";
            _logger.LogTradeEntry(result.Position, STRATEGY_NAME, signalDetails);
        }

        #endregion

        #region Position Management

        private void ManageOpenPositions(Position[] positions)
        {
            if (!UseTrailingStop)
            {
                return;
            }

            foreach (var position in positions)
            {
                double entryPrice = position.EntryPrice;
                double currentPrice = position.TradeType == TradeType.Buy ? Symbol.Bid : Symbol.Ask;
                double stopLoss = position.StopLoss ?? 0;

                if (stopLoss == 0)
                {
                    continue; // No stop loss set
                }

                double riskPips = Math.Abs(entryPrice - stopLoss) / Symbol.PipSize;
                double profitPips = position.TradeType == TradeType.Buy ?
                    (currentPrice - entryPrice) / Symbol.PipSize :
                    (entryPrice - currentPrice) / Symbol.PipSize;

                // Move to breakeven when profit reaches trigger
                if (profitPips >= riskPips * BreakevenTrigger)
                {
                    // Check if stop loss is not already at or beyond breakeven
                    bool needsUpdate = false;

                    if (position.TradeType == TradeType.Buy && stopLoss < entryPrice)
                    {
                        needsUpdate = true;
                    }
                    else if (position.TradeType == TradeType.Sell && stopLoss > entryPrice)
                    {
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        double breakevenPrice = entryPrice + (2 * Symbol.PipSize); // Small buffer for spread
                        _errorHandler.ModifyPositionSafely(position, breakevenPrice, position.TakeProfit);
                        _logger.LogDebug($"Moved stop loss to breakeven for position {position.Id}");
                    }
                }
            }
        }

        private double FindSwingLow(int lookback)
        {
            double swingLow = double.MaxValue;

            for (int i = 0; i < lookback && i < Bars.LowPrices.Count; i++)
            {
                double low = Bars.LowPrices.Last(i);
                if (low < swingLow)
                {
                    swingLow = low;
                }
            }

            return swingLow;
        }

        private double FindSwingHigh(int lookback)
        {
            double swingHigh = double.MinValue;

            for (int i = 0; i < lookback && i < Bars.HighPrices.Count; i++)
            {
                double high = Bars.HighPrices.Last(i);
                if (high > swingHigh)
                {
                    swingHigh = high;
                }
            }

            return swingHigh;
        }

        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            var position = args.Position;
            if (position.Label == POSITION_LABEL)
            {
                Print($"\n[POSITION OPENED] {position.TradeType} {position.VolumeInUnits} units @ {position.EntryPrice:F5}");
                Print($"  SL: {position.StopLoss:F5} | TP: {position.TakeProfit:F5}");
                Print($"  Risk-Reward: 1:{RiskRewardRatio}");
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
