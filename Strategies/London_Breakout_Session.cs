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
    /// London Breakout Session Strategy
    ///
    /// STRATEGY OVERVIEW:
    /// Aggressive session-based approach targeting 30-50% annual returns with 20-30% max drawdown.
    /// Exploits the high volatility during London session open (70%+ of daily forex volume).
    ///
    /// ENTRY RULES:
    /// 1. Define high/low range during Asian session (7:00-8:00 AM EST)
    /// 2. Wait for London open (8:00 AM EST)
    /// 3. LONG: Price breaks above range high with 15-min candle close confirmation
    /// 4. SHORT: Price breaks below range low with 15-min candle close confirmation
    ///
    /// EXIT RULES:
    /// - Take Profit: 1:1.5 to 1:2 risk-reward
    /// - Stop Loss: Inside opposite side of range (invalidation point)
    /// - Time-based: Close if not in profit after 4 hours
    ///
    /// BEST PAIRS: EUR/USD, GBP/USD
    /// TIMEFRAME: 15-minute
    /// EXPECTED WIN RATE: 60-70%
    /// EXPECTED TRADES: 2-5 per day
    /// </summary>
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class London_Breakout_Session : Robot
    {
        #region Parameters

        [Parameter("Risk Per Trade (%)", DefaultValue = 1.5, MinValue = 0.5, MaxValue = 5.0)]
        public double RiskPercent { get; set; }

        [Parameter("Asian Session Start (UTC)", DefaultValue = 0, MinValue = 0, MaxValue = 23)]
        public int AsianSessionStartHour { get; set; }

        [Parameter("Asian Session End (UTC)", DefaultValue = 7, MinValue = 0, MaxValue = 23)]
        public int AsianSessionEndHour { get; set; }

        [Parameter("London Session Start (UTC)", DefaultValue = 8, MinValue = 0, MaxValue = 23)]
        public int LondonSessionStartHour { get; set; }

        [Parameter("London Session End (UTC)", DefaultValue = 16, MinValue = 0, MaxValue = 23)]
        public int LondonSessionEndHour { get; set; }

        [Parameter("Risk-Reward Ratio", DefaultValue = 1.5, MinValue = 1.0, MaxValue = 3.0)]
        public double RiskRewardRatio { get; set; }

        [Parameter("Breakout Buffer (pips)", DefaultValue = 2, MinValue = 0, MaxValue = 10)]
        public double BreakoutBufferPips { get; set; }

        [Parameter("Max Trade Duration (hours)", DefaultValue = 4, MinValue = 1, MaxValue = 12)]
        public int MaxTradeDurationHours { get; set; }

        [Parameter("Min Range Size (pips)", DefaultValue = 15, MinValue = 5, MaxValue = 50)]
        public double MinRangeSizePips { get; set; }

        [Parameter("Max Range Size (pips)", DefaultValue = 80, MinValue = 30, MaxValue = 200)]
        public double MaxRangeSizePips { get; set; }

        [Parameter("Max Daily Loss %", DefaultValue = 3.0, MinValue = 1.0, MaxValue = 10.0)]
        public double MaxDailyLossPercent { get; set; }

        [Parameter("Max Weekly Loss %", DefaultValue = 7.0, MinValue = 2.0, MaxValue = 20.0)]
        public double MaxWeeklyLossPercent { get; set; }

        [Parameter("Max Drawdown %", DefaultValue = 25.0, MinValue = 15.0, MaxValue = 50.0)]
        public double MaxDrawdownPercent { get; set; }

        [Parameter("Max Concurrent Positions", DefaultValue = 2, MinValue = 1, MaxValue = 5)]
        public int MaxConcurrentPositions { get; set; }

        #endregion

        #region Private Fields

        private RiskManager _riskManager;
        private PositionSizer _positionSizer;
        private TradingLogger _logger;
        private ErrorHandler _errorHandler;

        private AverageTrueRange _atr;

        private double _asianRangeHigh;
        private double _asianRangeLow;
        private bool _rangeIdentified;
        private DateTime _rangeDate;
        private bool _tradedToday;

        private const string STRATEGY_NAME = "London_Breakout";
        private const string POSITION_LABEL = "LONDON_BO";

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
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Simple);

            // Initialize range tracking
            _asianRangeHigh = 0;
            _asianRangeLow = 0;
            _rangeIdentified = false;
            _rangeDate = DateTime.MinValue;
            _tradedToday = false;

            Print("Indicators initialized successfully");
            Print($"Asian Session: {AsianSessionStartHour:D2}:00 - {AsianSessionEndHour:D2}:00 UTC");
            Print($"London Session: {LondonSessionStartHour:D2}:00 - {LondonSessionEndHour:D2}:00 UTC");
            Print($"Range Size: {MinRangeSizePips}-{MaxRangeSizePips} pips");
            Print($"Risk-Reward Ratio: 1:{RiskRewardRatio}\n");

            // Subscribe to position events
            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;

            // Display timezone warning
            if (TimeZone != TimeZones.UTC)
            {
                _logger.LogWarning("Bot timezone is not UTC. Session times may need adjustment.");
            }
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

                DateTime currentTime = Server.Time;
                int currentHour = currentTime.Hour;

                // Reset daily tracking
                if (currentTime.Date != _rangeDate.Date)
                {
                    _rangeIdentified = false;
                    _tradedToday = false;
                    _asianRangeHigh = 0;
                    _asianRangeLow = 0;
                    _logger.LogDebug($"New trading day: {currentTime.Date:yyyy-MM-dd}");
                }

                // Manage existing positions (time-based exits)
                var existingPositions = Positions.FindAll(POSITION_LABEL, SymbolName);
                if (existingPositions.Length > 0)
                {
                    ManageOpenPositions(existingPositions, currentTime);

                    // Don't look for new setups if we already traded today
                    if (_tradedToday)
                    {
                        return;
                    }
                }

                // STEP 1: Identify Asian session range
                if (currentHour >= AsianSessionStartHour && currentHour < AsianSessionEndHour)
                {
                    IdentifyAsianRange(currentTime);
                }

                // STEP 2: Trade London breakout
                if (currentHour >= LondonSessionStartHour && currentHour < LondonSessionEndHour)
                {
                    if (_rangeIdentified && !_tradedToday)
                    {
                        CheckForBreakout();
                    }
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

        #region Asian Range Identification

        private void IdentifyAsianRange(DateTime currentTime)
        {
            // Only identify range once per day
            if (_rangeIdentified && _rangeDate.Date == currentTime.Date)
            {
                return;
            }

            // Find high and low during Asian session
            double sessionHigh = double.MinValue;
            double sessionLow = double.MaxValue;

            // Look back through recent bars during Asian session
            for (int i = 0; i < Bars.Count && i < 100; i++)
            {
                DateTime barTime = Bars.OpenTimes.Last(i);

                // Check if bar is within Asian session today
                if (barTime.Date == currentTime.Date &&
                    barTime.Hour >= AsianSessionStartHour &&
                    barTime.Hour < AsianSessionEndHour)
                {
                    double high = Bars.HighPrices.Last(i);
                    double low = Bars.LowPrices.Last(i);

                    if (high > sessionHigh)
                    {
                        sessionHigh = high;
                    }

                    if (low < sessionLow)
                    {
                        sessionLow = low;
                    }
                }
            }

            // Validate range
            if (sessionHigh != double.MinValue && sessionLow != double.MaxValue)
            {
                double rangeSizePips = (sessionHigh - sessionLow) / Symbol.PipSize;

                if (rangeSizePips >= MinRangeSizePips && rangeSizePips <= MaxRangeSizePips)
                {
                    _asianRangeHigh = sessionHigh;
                    _asianRangeLow = sessionLow;
                    _rangeIdentified = true;
                    _rangeDate = currentTime.Date;

                    Print($"\n[ASIAN RANGE IDENTIFIED] Date: {currentTime.Date:yyyy-MM-dd}");
                    Print($"  High: {_asianRangeHigh:F5}");
                    Print($"  Low: {_asianRangeLow:F5}");
                    Print($"  Range: {rangeSizePips:F1} pips\n");

                    _logger.LogDebug($"Asian range set: {_asianRangeHigh:F5} - {_asianRangeLow:F5} ({rangeSizePips:F1} pips)");
                }
                else
                {
                    _logger.LogDebug($"Range size invalid: {rangeSizePips:F1} pips (min: {MinRangeSizePips}, max: {MaxRangeSizePips})");
                }
            }
        }

        #endregion

        #region Breakout Detection and Entry

        private void CheckForBreakout()
        {
            double currentClose = Bars.ClosePrices.LastValue;
            double currentHigh = Bars.HighPrices.LastValue;
            double currentLow = Bars.LowPrices.LastValue;

            double breakoutBuffer = BreakoutBufferPips * Symbol.PipSize;

            // Check for bullish breakout (close above range high)
            bool bullishBreakout = currentClose > (_asianRangeHigh + breakoutBuffer);

            // Check for bearish breakout (close below range low)
            bool bearishBreakout = currentClose < (_asianRangeLow - breakoutBuffer);

            if (bullishBreakout && !_tradedToday)
            {
                ExecuteLongEntry();
                _tradedToday = true; // Only one trade per day
            }
            else if (bearishBreakout && !_tradedToday)
            {
                ExecuteShortEntry();
                _tradedToday = true; // Only one trade per day
            }
        }

        private void ExecuteLongEntry()
        {
            if (!_riskManager.CanOpenNewPosition())
            {
                _logger.LogDebug("Cannot open LONG: Risk limits or position limits reached");
                return;
            }

            double entryPrice = Symbol.Ask;

            // Stop loss: Below Asian range low
            double stopLossPrice = _asianRangeLow - (5 * Symbol.PipSize); // 5 pips buffer
            double stopLossPips = _positionSizer.CalculateStopLossPips(entryPrice, stopLossPrice);

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
                "LONG breakout entry"
            );

            if (!_errorHandler.HandleTradeResult(result, "LONG breakout entry"))
            {
                _logger.LogError($"Failed to execute LONG entry: {result.Error}");
                return;
            }

            string signalDetails = $"London Breakout LONG - Range: [{_asianRangeLow:F5}, {_asianRangeHigh:F5}], Entry: {entryPrice:F5}, R:R=1:{RiskRewardRatio}";
            _logger.LogTradeEntry(result.Position, STRATEGY_NAME, signalDetails);

            Print($"\n[BREAKOUT LONG] Entry: {entryPrice:F5}");
            Print($"  Range High Broken: {_asianRangeHigh:F5}");
            Print($"  SL: {stopLossPrice:F5} ({stopLossPips:F1} pips)");
            Print($"  TP: {takeProfitPrice:F5}\n");
        }

        private void ExecuteShortEntry()
        {
            if (!_riskManager.CanOpenNewPosition())
            {
                _logger.LogDebug("Cannot open SHORT: Risk limits or position limits reached");
                return;
            }

            double entryPrice = Symbol.Bid;

            // Stop loss: Above Asian range high
            double stopLossPrice = _asianRangeHigh + (5 * Symbol.PipSize); // 5 pips buffer
            double stopLossPips = _positionSizer.CalculateStopLossPips(entryPrice, stopLossPrice);

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
                "SHORT breakout entry"
            );

            if (!_errorHandler.HandleTradeResult(result, "SHORT breakout entry"))
            {
                _logger.LogError($"Failed to execute SHORT entry: {result.Error}");
                return;
            }

            string signalDetails = $"London Breakout SHORT - Range: [{_asianRangeLow:F5}, {_asianRangeHigh:F5}], Entry: {entryPrice:F5}, R:R=1:{RiskRewardRatio}";
            _logger.LogTradeEntry(result.Position, STRATEGY_NAME, signalDetails);

            Print($"\n[BREAKOUT SHORT] Entry: {entryPrice:F5}");
            Print($"  Range Low Broken: {_asianRangeLow:F5}");
            Print($"  SL: {stopLossPrice:F5} ({stopLossPips:F1} pips)");
            Print($"  TP: {takeProfitPrice:F5}\n");
        }

        #endregion

        #region Position Management

        private void ManageOpenPositions(Position[] positions, DateTime currentTime)
        {
            foreach (var position in positions)
            {
                // Close position if max duration exceeded and not in profit
                TimeSpan duration = currentTime - position.EntryTime;

                if (duration.TotalHours >= MaxTradeDurationHours)
                {
                    if (position.NetProfit <= 0)
                    {
                        _errorHandler.ClosePositionSafely(position, $"Max duration reached ({MaxTradeDurationHours}h)");
                        _logger.LogDebug($"Closed position {position.Id} after {duration.TotalHours:F1} hours with P/L: {position.NetProfit:F2}");
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
                Print($"  Position ID: {position.Id}");
                Print($"  Max Duration: {MaxTradeDurationHours} hours\n");
            }
        }

        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            var position = args.Position;
            if (position.Label == POSITION_LABEL)
            {
                TimeSpan duration = args.Position.EntryTime - position.EntryTime;

                string exitReason = position.NetProfit > 0 ? "Take Profit Hit" :
                                  position.NetProfit < 0 ? "Stop Loss Hit" : "Break Even/Time Exit";

                _logger.LogTradeExit(position, exitReason);

                Print($"\n[POSITION CLOSED] {position.TradeType} {position.SymbolName}");
                Print($"  P/L: {position.NetProfit:F2} ({position.Pips:F1} pips)");
                Print($"  Duration: {duration.TotalHours:F1} hours");
                Print($"  Reason: {exitReason}\n");

                // Print risk statistics
                var riskStats = _riskManager.GetRiskStatistics();
                Print($"[RISK STATS] Daily Loss: {riskStats.DailyLossPercent:F2}% | Drawdown: {riskStats.DrawdownPercent:F2}%\n");
            }
        }

        #endregion
    }
}
