using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using TradingBot.Framework.RiskManagement;
using TradingBot.Framework.Logging;
using TradingBot.Framework.ErrorHandling;

namespace TradingBot.Examples
{
    /// <summary>
    /// EXAMPLE 1: Simple Strategy Template
    ///
    /// This is a minimal, well-structured trading bot template that demonstrates:
    /// - Proper framework integration
    /// - Clean parameter organization
    /// - Safe entry/exit logic
    /// - Comprehensive logging
    ///
    /// USE THIS AS A STARTING POINT for building your own strategies.
    ///
    /// LEARNING OBJECTIVES:
    /// 1. Understand basic cTrader bot structure
    /// 2. Learn how to integrate the framework components
    /// 3. See proper error handling patterns
    /// 4. Understand OnStart(), OnBar(), OnStop() lifecycle
    /// </summary>
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SimpleStrategyTemplate : Robot
    {
        #region Parameters

        // === RISK PARAMETERS (Most Important!) ===

        [Parameter("Risk Per Trade (%)", DefaultValue = 1.0, MinValue = 0.1, MaxValue = 5.0)]
        public double RiskPercent { get; set; }

        [Parameter("Max Daily Loss %", DefaultValue = 2.0, MinValue = 1.0, MaxValue = 10.0)]
        public double MaxDailyLossPercent { get; set; }

        [Parameter("Max Drawdown %", DefaultValue = 20.0, MinValue = 10.0, MaxValue = 50.0)]
        public double MaxDrawdownPercent { get; set; }

        // === STRATEGY PARAMETERS ===

        [Parameter("Stop Loss (pips)", DefaultValue = 50, MinValue = 10, MaxValue = 200)]
        public double StopLossPips { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 100, MinValue = 20, MaxValue = 400)]
        public double TakeProfitPips { get; set; }

        #endregion

        #region Private Fields

        // Framework components
        private RiskManager _riskManager;
        private PositionSizer _positionSizer;
        private TradingLogger _logger;
        private ErrorHandler _errorHandler;

        // Strategy-specific fields
        private const string POSITION_LABEL = "SIMPLE_TEMPLATE";

        #endregion

        /// <summary>
        /// OnStart() is called once when the bot starts
        /// Initialize all indicators, framework components, and settings here
        /// </summary>
        protected override void OnStart()
        {
            Print("=== Simple Strategy Template Starting ===");

            // Step 1: Initialize framework components
            // These provide risk management, logging, and error handling

            _riskManager = new RiskManager(
                this,
                MaxDailyLossPercent,    // Daily loss limit
                5.0,                     // Weekly loss limit (hardcoded 5%)
                MaxDrawdownPercent,      // Maximum drawdown from peak
                5                        // Max concurrent positions
            );

            _positionSizer = new PositionSizer(this, Symbol);
            _logger = new TradingLogger(this);
            _errorHandler = new ErrorHandler(this, maxRetries: 3);

            // Step 2: Initialize indicators (if any)
            // Example: _rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, 14);

            // Step 3: Subscribe to events
            Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;

            // Step 4: Log initialization
            _logger.LogDebug($"Bot initialized on {SymbolName} {TimeFrame}");
            _logger.LogDebug($"Risk per trade: {RiskPercent}%, Max drawdown: {MaxDrawdownPercent}%");

            Print("Initialization complete!");
        }

        /// <summary>
        /// OnBar() is called when a new bar forms on your selected timeframe
        /// This is where your main trading logic goes
        ///
        /// IMPORTANT:
        /// - OnBar is more efficient than OnTick for most strategies
        /// - Only use OnTick for scalping or time-sensitive operations
        /// </summary>
        protected override void OnBar()
        {
            try
            {
                // STEP 1: Check risk limits FIRST
                // Never trade if risk limits are breached
                if (!_riskManager.CheckRiskLimits())
                {
                    _logger.LogWarning("Risk limits breached, trading paused");
                    return;
                }

                // STEP 2: Check if we already have a position
                // This template allows only 1 position at a time
                var existingPosition = Positions.Find(POSITION_LABEL, SymbolName);
                if (existingPosition != null)
                {
                    // Could manage position here (trailing stops, etc.)
                    return;
                }

                // STEP 3: Generate trading signal
                // This is where YOUR strategy logic goes
                bool longSignal = CheckForLongSignal();
                bool shortSignal = CheckForShortSignal();

                // STEP 4: Execute trades based on signals
                if (longSignal)
                {
                    ExecuteLongTrade();
                }
                else if (shortSignal)
                {
                    ExecuteShortTrade();
                }
            }
            catch (Exception ex)
            {
                // Always catch exceptions to prevent bot crashes
                _logger.LogError($"Error in OnBar: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// OnStop() is called when the bot stops
        /// Use this to print final statistics and export data
        /// </summary>
        protected override void OnStop()
        {
            Print("\n=== Simple Strategy Template Stopping ===");

            // Print performance report
            _logger.PrintPerformanceReport();

            // Export trade logs for analysis
            string csv = _logger.ExportToCSV();
            Print("\nTrade Log (CSV format):");
            Print(csv);

            Print("=== Bot Stopped ===\n");
        }

        #region Signal Logic (YOUR STRATEGY GOES HERE)

        /// <summary>
        /// Check if conditions are right for a LONG trade
        ///
        /// CUSTOMIZE THIS: Replace with your own entry conditions
        /// Examples:
        /// - Price crosses above moving average
        /// - RSI oversold + price at support
        /// - Breakout above resistance
        /// </summary>
        private bool CheckForLongSignal()
        {
            // EXAMPLE: Simple moving average crossover
            // Replace this with your actual strategy logic

            // For demonstration, we'll return false (no signal)
            // In a real strategy, you'd check your indicators here

            return false;

            /* EXAMPLE IMPLEMENTATION:
            double fastMA = _fastMA.Result.LastValue;
            double slowMA = _slowMA.Result.LastValue;
            double prevFastMA = _fastMA.Result.Last(1);
            double prevSlowMA = _slowMA.Result.Last(1);

            // Bullish crossover: fast MA crosses above slow MA
            bool bullishCross = prevFastMA <= prevSlowMA && fastMA > slowMA;

            return bullishCross;
            */
        }

        /// <summary>
        /// Check if conditions are right for a SHORT trade
        ///
        /// CUSTOMIZE THIS: Replace with your own entry conditions
        /// </summary>
        private bool CheckForShortSignal()
        {
            // EXAMPLE: Return false for now
            // In a real strategy, you'd check your indicators here

            return false;

            /* EXAMPLE IMPLEMENTATION:
            double fastMA = _fastMA.Result.LastValue;
            double slowMA = _slowMA.Result.LastValue;
            double prevFastMA = _fastMA.Result.Last(1);
            double prevSlowMA = _slowMA.Result.Last(1);

            // Bearish crossover: fast MA crosses below slow MA
            bool bearishCross = prevFastMA >= prevSlowMA && fastMA < slowMA;

            return bearishCross;
            */
        }

        #endregion

        #region Trade Execution

        /// <summary>
        /// Execute a LONG trade with proper risk management
        ///
        /// STEPS:
        /// 1. Check if we can open position (risk limits)
        /// 2. Calculate position size based on risk
        /// 3. Validate order parameters
        /// 4. Execute with error handling
        /// 5. Log trade details
        /// </summary>
        private void ExecuteLongTrade()
        {
            // Step 1: Check if we can open a new position
            if (!_riskManager.CanOpenNewPosition())
            {
                _logger.LogDebug("Cannot open LONG: Risk limits reached");
                return;
            }

            // Step 2: Calculate position size
            // This automatically adjusts size based on stop loss and risk percentage
            double positionSize = _positionSizer.CalculatePositionSizeByRisk(
                RiskPercent,
                StopLossPips
            );

            // Step 3: Validate position size
            if (!_positionSizer.ValidatePositionSize(positionSize))
            {
                _logger.LogError("Invalid position size for LONG trade");
                return;
            }

            // Step 4: Calculate stop loss and take profit prices
            double entryPrice = Symbol.Ask;
            double stopLossPrice = entryPrice - (StopLossPips * Symbol.PipSize);
            double takeProfitPrice = entryPrice + (TakeProfitPips * Symbol.PipSize);

            // Step 5: Validate order parameters
            if (!_errorHandler.ValidateOrderParameters(Symbol, positionSize, stopLossPrice, takeProfitPrice, TradeType.Buy))
            {
                _logger.LogError("Order validation failed for LONG trade");
                return;
            }

            // Step 6: Execute trade with automatic retry on transient errors
            var result = _errorHandler.ExecuteWithRetry(
                () => ExecuteMarketOrder(TradeType.Buy, SymbolName, positionSize, POSITION_LABEL, StopLossPips, TakeProfitPips),
                "LONG entry"
            );

            // Step 7: Handle result
            if (!_errorHandler.HandleTradeResult(result, "LONG entry"))
            {
                _logger.LogError($"Failed to execute LONG trade: {result.Error}");
                return;
            }

            // Step 8: Log successful trade
            _logger.LogTradeEntry(
                result.Position,
                "SimpleTemplate",
                $"Long entry at {entryPrice:F5}, SL: {stopLossPrice:F5}, TP: {takeProfitPrice:F5}"
            );

            Print($"[LONG OPENED] Size: {positionSize} units, Entry: {entryPrice:F5}");
        }

        /// <summary>
        /// Execute a SHORT trade with proper risk management
        ///
        /// Same steps as LONG trade, but in opposite direction
        /// </summary>
        private void ExecuteShortTrade()
        {
            // Step 1: Check if we can open a new position
            if (!_riskManager.CanOpenNewPosition())
            {
                _logger.LogDebug("Cannot open SHORT: Risk limits reached");
                return;
            }

            // Step 2: Calculate position size
            double positionSize = _positionSizer.CalculatePositionSizeByRisk(
                RiskPercent,
                StopLossPips
            );

            // Step 3: Validate position size
            if (!_positionSizer.ValidatePositionSize(positionSize))
            {
                _logger.LogError("Invalid position size for SHORT trade");
                return;
            }

            // Step 4: Calculate stop loss and take profit prices
            double entryPrice = Symbol.Bid;
            double stopLossPrice = entryPrice + (StopLossPips * Symbol.PipSize);
            double takeProfitPrice = entryPrice - (TakeProfitPips * Symbol.PipSize);

            // Step 5: Validate order parameters
            if (!_errorHandler.ValidateOrderParameters(Symbol, positionSize, stopLossPrice, takeProfitPrice, TradeType.Sell))
            {
                _logger.LogError("Order validation failed for SHORT trade");
                return;
            }

            // Step 6: Execute trade with automatic retry on transient errors
            var result = _errorHandler.ExecuteWithRetry(
                () => ExecuteMarketOrder(TradeType.Sell, SymbolName, positionSize, POSITION_LABEL, StopLossPips, TakeProfitPips),
                "SHORT entry"
            );

            // Step 7: Handle result
            if (!_errorHandler.HandleTradeResult(result, "SHORT entry"))
            {
                _logger.LogError($"Failed to execute SHORT trade: {result.Error}");
                return;
            }

            // Step 8: Log successful trade
            _logger.LogTradeEntry(
                result.Position,
                "SimpleTemplate",
                $"Short entry at {entryPrice:F5}, SL: {stopLossPrice:F5}, TP: {takeProfitPrice:F5}"
            );

            Print($"[SHORT OPENED] Size: {positionSize} units, Entry: {entryPrice:F5}");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when a position is opened
        /// Use for logging, notifications, or initial management
        /// </summary>
        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            var position = args.Position;
            if (position.Label == POSITION_LABEL)
            {
                Print($"\n[POSITION OPENED]");
                Print($"  Type: {position.TradeType}");
                Print($"  Size: {position.VolumeInUnits} units");
                Print($"  Entry: {position.EntryPrice:F5}");
                Print($"  SL: {position.StopLoss:F5}");
                Print($"  TP: {position.TakeProfit:F5}\n");
            }
        }

        /// <summary>
        /// Called when a position is closed
        /// Use for logging final results
        /// </summary>
        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            var position = args.Position;
            if (position.Label == POSITION_LABEL)
            {
                // Determine exit reason
                string exitReason = position.NetProfit > 0 ? "Take Profit" :
                                  position.NetProfit < 0 ? "Stop Loss" :
                                  "Break Even";

                // Log trade exit
                _logger.LogTradeExit(position, exitReason);

                // Print summary
                Print($"\n[POSITION CLOSED]");
                Print($"  Type: {position.TradeType}");
                Print($"  P/L: {position.NetProfit:F2} ({position.Pips:F1} pips)");
                Print($"  Reason: {exitReason}");

                // Print risk statistics
                var riskStats = _riskManager.GetRiskStatistics();
                Print($"  Daily Loss: {riskStats.DailyLossPercent:F2}%");
                Print($"  Drawdown: {riskStats.DrawdownPercent:F2}%\n");
            }
        }

        #endregion
    }
}

/*
 * ========================================
 * HOW TO USE THIS TEMPLATE
 * ========================================
 *
 * 1. Copy this entire file
 * 2. Rename the class (line 25) to your strategy name
 * 3. Add your indicators in OnStart():
 *    Example: _myIndicator = Indicators.SomeIndicator(...);
 *
 * 4. Implement your entry logic in CheckForLongSignal() and CheckForShortSignal()
 *    Example: Check if RSI < 30 and price > EMA200
 *
 * 5. Customize parameters as needed
 *    Add new parameters for your indicators
 *
 * 6. Test on BACKTEST first:
 *    - Use tick data
 *    - Include realistic spread/commission
 *    - Test for 6-12 months minimum
 *
 * 7. Validate results:
 *    - Check for overfitting
 *    - Run Monte Carlo simulation
 *    - Test on demo for 2-3 months
 *
 * 8. Only then consider live trading
 *
 * ========================================
 * KEY LEARNING POINTS
 * ========================================
 *
 * ✓ Always check risk limits BEFORE trading
 * ✓ Use framework components for safety
 * ✓ Calculate position size based on risk, not fixed lots
 * ✓ Validate all parameters before execution
 * ✓ Use error handling with retries
 * ✓ Log everything for analysis
 * ✓ Never remove stop losses
 * ✓ Test thoroughly before live
 *
 * ========================================
 * COMMON MISTAKES TO AVOID
 * ========================================
 *
 * ✗ Removing or ignoring risk limits
 * ✗ Using fixed lot sizes (0.1 lots always)
 * ✗ Not validating order parameters
 * ✗ Skipping error handling
 * ✗ Trading without stop losses
 * ✗ Not logging trades
 * ✗ Going live without demo testing
 * ✗ Overriding bot during drawdowns
 *
 * ========================================
 */
