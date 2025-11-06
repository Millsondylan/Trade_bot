/*
 * ========================================
 * EXAMPLE 2: Common Pitfalls and How to Fix Them
 * ========================================
 *
 * This file demonstrates COMMON MISTAKES that beginners make
 * and shows the CORRECT way to implement each feature.
 *
 * LEARNING OBJECTIVES:
 * - Understand what NOT to do
 * - Learn the correct patterns
 * - Avoid account-destroying mistakes
 * - Build robust, safe trading bots
 *
 * ========================================
 */

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using TradingBot.Framework.RiskManagement;

namespace TradingBot.Examples
{
    // ========================================
    // PITFALL #1: No Risk Management
    // ========================================

    /*
     * WRONG: No position sizing, no risk limits
     */
    public class BadExample_NoRiskManagement : Robot
    {
        protected override void OnBar()
        {
            // ✗ WRONG: Fixed lot size regardless of account
            double volume = 10000; // Always 0.1 lots

            // ✗ WRONG: No stop loss!
            ExecuteMarketOrder(TradeType.Buy, SymbolName, volume, "BAD");

            // PROBLEMS:
            // - Same position size on $1K and $100K account = disaster
            // - No stop loss = one bad trade can wipe account
            // - No drawdown limits = can lose everything
        }
    }

    /*
     * CORRECT: Proper risk management
     */
    public class GoodExample_RiskManagement : Robot
    {
        private RiskManager _riskManager;
        private PositionSizer _positionSizer;

        [Parameter("Risk Per Trade (%)", DefaultValue = 1.0, MaxValue = 2.0)]
        public double RiskPercent { get; set; }

        protected override void OnStart()
        {
            // ✓ CORRECT: Initialize risk manager
            _riskManager = new RiskManager(
                this,
                maxDailyLossPercent: 2.0,
                maxWeeklyLossPercent: 5.0,
                maxDrawdownPercent: 20.0,
                maxConcurrentPositions: 5
            );

            _positionSizer = new PositionSizer(this, Symbol);
        }

        protected override void OnBar()
        {
            // ✓ CORRECT: Check risk limits first
            if (!_riskManager.CheckRiskLimits())
            {
                return; // Don't trade if limits breached
            }

            // ✓ CORRECT: Calculate position size based on risk
            double stopLossPips = 50;
            double positionSize = _positionSizer.CalculatePositionSizeByRisk(
                RiskPercent,
                stopLossPips
            );

            // ✓ CORRECT: Always include stop loss
            ExecuteMarketOrder(TradeType.Buy, SymbolName, positionSize, "GOOD", stopLossPips, null);

            // BENEFITS:
            // - Position size scales with account
            // - Risk is controlled per trade
            // - Drawdown limits prevent catastrophic loss
            // - Stop loss protects each trade
        }
    }

    // ========================================
    // PITFALL #2: Indicator Initialization in Wrong Place
    // ========================================

    /*
     * WRONG: Creating indicators inside OnBar()
     */
    public class BadExample_IndicatorInit : Robot
    {
        protected override void OnBar()
        {
            // ✗ WRONG: Creating new indicator every bar
            var rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, 14);
            double rsiValue = rsi.Result.LastValue;

            // PROBLEMS:
            // - Creates new indicator object every bar
            // - Massive memory leak
            // - Very slow performance
            // - Will crash after hours of running
        }
    }

    /*
     * CORRECT: Initialize indicators in OnStart()
     */
    public class GoodExample_IndicatorInit : Robot
    {
        private RelativeStrengthIndex _rsi; // Store as field

        protected override void OnStart()
        {
            // ✓ CORRECT: Create indicator once in OnStart()
            _rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, 14);
        }

        protected override void OnBar()
        {
            // ✓ CORRECT: Just read the value
            double rsiValue = _rsi.Result.LastValue;

            // BENEFITS:
            // - No memory leaks
            // - Fast performance
            // - Stable long-term operation
        }
    }

    // ========================================
    // PITFALL #3: Modifying Collections While Iterating
    // ========================================

    /*
     * WRONG: Closing positions while iterating
     */
    public class BadExample_ModifyingCollection : Robot
    {
        protected override void OnBar()
        {
            // ✗ WRONG: Modifying collection during iteration
            foreach (var position in Positions)
            {
                if (position.NetProfit > 100)
                {
                    position.Close(); // CRASH! Collection modified during iteration
                }
            }

            // PROBLEM:
            // - Positions collection changes while iterating
            // - Causes "Collection was modified" exception
            // - Bot crashes
        }
    }

    /*
     * CORRECT: Create copy with ToArray()
     */
    public class GoodExample_SafeIteration : Robot
    {
        protected override void OnBar()
        {
            // ✓ CORRECT: Create snapshot with ToArray()
            foreach (var position in Positions.ToArray())
            {
                if (position.NetProfit > 100)
                {
                    position.Close(); // Safe! Working on copy
                }
            }

            // BENEFITS:
            // - No crashes
            // - Safe to modify positions
            // - Reliable operation
        }
    }

    // ========================================
    // PITFALL #4: Not Checking If Position Exists
    // ========================================

    /*
     * WRONG: Assuming position exists
     */
    public class BadExample_NoPositionCheck : Robot
    {
        protected override void OnBar()
        {
            // ✗ WRONG: Assuming position exists
            var position = Positions.Find("LABEL", SymbolName);

            position.ModifyStopLossPips(50, null); // CRASH if position is null!

            // PROBLEM:
            // - position could be null (already closed, never existed)
            // - NullReferenceException crash
        }
    }

    /*
     * CORRECT: Check for null first
     */
    public class GoodExample_PositionCheck : Robot
    {
        protected override void OnBar()
        {
            // ✓ CORRECT: Check if position exists
            var position = Positions.Find("LABEL", SymbolName);

            if (position != null)
            {
                position.ModifyStopLossPips(50, null); // Safe!
            }

            // OR use null-conditional operator
            position?.ModifyStopLossPips(50, null);

            // BENEFITS:
            // - No crashes
            // - Handles all cases
        }
    }

    // ========================================
    // PITFALL #5: Over-Optimization (Curve Fitting)
    // ========================================

    /*
     * WRONG: Too many parameters, optimized on full dataset
     */
    public class BadExample_OverOptimized : Robot
    {
        // ✗ WRONG: Too many parameters to optimize
        [Parameter("MA Period 1")] public int MA1 { get; set; }
        [Parameter("MA Period 2")] public int MA2 { get; set; }
        [Parameter("MA Period 3")] public int MA3 { get; set; }
        [Parameter("RSI Period")] public int RSIPeriod { get; set; }
        [Parameter("RSI Oversold")] public int RSIOversold { get; set; }
        [Parameter("RSI Overbought")] public int RSIOverbought { get; set; }
        [Parameter("ATR Period")] public int ATRPeriod { get; set; }
        [Parameter("ATR Multiplier")] public double ATRMult { get; set; }
        [Parameter("BB Period")] public int BBPeriod { get; set; }
        [Parameter("BB StdDev")] public double BBStdDev { get; set; }
        [Parameter("ADX Period")] public int ADXPeriod { get; set; }
        [Parameter("ADX Min")] public double ADXMin { get; set; }
        // ... and 20 more parameters!

        // PROBLEMS:
        // - Optimizing 10+ parameters = guaranteed overfitting
        // - Works perfectly on backtest
        // - Fails completely on live
        // - Classic beginner mistake
    }

    /*
     * CORRECT: Limit to 2-3 parameters maximum
     */
    public class GoodExample_SimpleRobust : Robot
    {
        // ✓ CORRECT: Only essential parameters
        [Parameter("Risk %", DefaultValue = 1.0)]
        public double RiskPercent { get; set; }

        [Parameter("EMA Period", DefaultValue = 21)]
        public int EMAPeriod { get; set; }

        [Parameter("Stop Loss Pips", DefaultValue = 50)]
        public double StopLossPips { get; set; }

        // BENEFITS:
        // - Fewer ways to overfit
        // - More robust across time periods
        // - Works on out-of-sample data
        // - Actually profitable live

        // RULE OF THUMB:
        // - Optimize 2-3 parameters max
        // - Select from plateau regions on heatmap
        // - Test on out-of-sample data
        // - Expect 10-30% performance degradation live
    }

    // ========================================
    // PITFALL #6: No Error Handling
    // ========================================

    /*
     * WRONG: No try-catch, no error handling
     */
    public class BadExample_NoErrorHandling : Robot
    {
        protected override void OnBar()
        {
            // ✗ WRONG: No error handling
            var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, 10000, "BAD");

            // What if:
            // - No money?
            // - Market closed?
            // - Connection lost?
            // - Invalid parameters?
            // Bot just crashes!
        }
    }

    /*
     * CORRECT: Comprehensive error handling
     */
    public class GoodExample_ErrorHandling : Robot
    {
        private TradingBot.Framework.ErrorHandling.ErrorHandler _errorHandler;

        protected override void OnStart()
        {
            _errorHandler = new TradingBot.Framework.ErrorHandling.ErrorHandler(this, maxRetries: 3);
        }

        protected override void OnBar()
        {
            try
            {
                // ✓ CORRECT: Validate parameters first
                double volume = 10000;
                if (!_errorHandler.ValidateOrderParameters(Symbol, volume, null, null, TradeType.Buy))
                {
                    Print("Invalid order parameters");
                    return;
                }

                // ✓ CORRECT: Execute with retry logic
                var result = _errorHandler.ExecuteWithRetry(
                    () => ExecuteMarketOrder(TradeType.Buy, SymbolName, volume, "GOOD"),
                    "Buy order"
                );

                // ✓ CORRECT: Handle result
                if (!_errorHandler.HandleTradeResult(result, "Buy order"))
                {
                    Print($"Trade failed: {result.Error}");
                    return;
                }

                Print("Trade executed successfully!");
            }
            catch (Exception ex)
            {
                // ✓ CORRECT: Catch unexpected errors
                Print($"ERROR: {ex.Message}");
            }

            // BENEFITS:
            // - Handles all error cases
            // - Automatic retry on transient errors
            // - Bot stays running
            // - Clear error messages for debugging
        }
    }

    // ========================================
    // PITFALL #7: Trading Without Testing
    // ========================================

    /*
     * WRONG: Deploy directly to live
     */
    public class BadExample_NoTesting
    {
        /*
         * WRONG SEQUENCE:
         * 1. Write strategy
         * 2. Deploy to live account immediately
         * 3. Lose money
         *
         * PROBLEMS:
         * - No idea if strategy works
         * - Bugs will appear in live
         * - Poor risk management
         * - Expensive "testing" on real money
         */
    }

    /*
     * CORRECT: Thorough testing sequence
     */
    public class GoodExample_Testing
    {
        /*
         * CORRECT SEQUENCE:
         *
         * 1. BACKTEST (1-2 hours)
         *    - Use TICK DATA
         *    - Include realistic spread/commission
         *    - Test 6-12 months minimum
         *    - Verify positive results
         *
         * 2. OUT-OF-SAMPLE TEST (1 hour)
         *    - Test on period NOT used for optimization
         *    - Compare to in-sample results
         *    - Accept if degradation <30%
         *
         * 3. MONTE CARLO (30 minutes)
         *    - Run 1000+ simulations
         *    - Check 95th percentile drawdown
         *    - Verify acceptable risk of ruin
         *
         * 4. DEMO ACCOUNT (2-3 months)
         *    - Test with virtual money
         *    - Minimum 50-100 trades
         *    - Verify execution quality
         *    - Check performance vs backtest
         *
         * 5. MICRO LIVE (1 month)
         *    - $500-1,000 capital
         *    - Smallest position sizes
         *    - Verify slippage/commission
         *    - Build confidence
         *
         * 6. LIVE (ongoing)
         *    - $5,000+ capital
         *    - Scale gradually
         *    - Monitor continuously
         *    - Adjust as needed
         *
         * TOTAL TIME: 3-4 months before serious live trading
         * BENEFIT: Avoid losing money on untested strategies
         */
    }

    // ========================================
    // PITFALL #8: Using OnTick Inappropriately
    // ========================================

    /*
     * WRONG: Using OnTick for everything
     */
    public class BadExample_OnTick : Robot
    {
        protected override void OnTick()
        {
            // ✗ WRONG: OnTick fires 100+ times per second
            var rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, 14);
            double rsiValue = rsi.Result.LastValue;

            if (rsiValue < 30)
            {
                ExecuteMarketOrder(TradeType.Buy, SymbolName, 10000, "BAD");
            }

            // PROBLEMS:
            // - OnTick fires constantly (wasted CPU)
            // - Creates indicators every tick (memory leak)
            // - May open multiple trades per second
            // - Competing with HFT firms (you'll lose)
        }
    }

    /*
     * CORRECT: Use OnBar for most strategies
     */
    public class GoodExample_OnBar : Robot
    {
        private RelativeStrengthIndex _rsi;
        private bool _tradedThisBar;

        protected override void OnStart()
        {
            _rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, 14);
        }

        protected override void OnBar()
        {
            // ✓ CORRECT: OnBar fires once per bar (efficient)
            _tradedThisBar = false;

            double rsiValue = _rsi.Result.LastValue;

            if (rsiValue < 30 && !_tradedThisBar)
            {
                ExecuteMarketOrder(TradeType.Buy, SymbolName, 10000, "GOOD");
                _tradedThisBar = true;
            }

            // BENEFITS:
            // - Efficient (fires once per timeframe)
            // - No duplicate trades
            // - Realistic for retail traders
            // - Better performance

            // NOTE: Only use OnTick for:
            // - Scalping strategies (<5 min trades)
            // - Time-sensitive operations
            // - High-frequency trading (if you have infrastructure)
        }
    }

    // ========================================
    // PITFALL #9: Ignoring Backtest vs Live Gap
    // ========================================

    /*
     * WRONG: Expecting backtest results to match live perfectly
     */
    public class BadExample_UnrealisticExpectations
    {
        /*
         * WRONG THINKING:
         * "Backtest shows 80% win rate and 500% annual return.
         *  I'll be rich in months!"
         *
         * REALITY:
         * - Backtest used 1-minute bars (not tick data)
         * - Spread was fixed (not variable)
         * - No commission included
         * - Optimized on full dataset
         * - Perfect equity curve = overfit
         *
         * LIVE RESULT:
         * - 40% win rate
         * - -20% annual return
         * - Account blown in 3 months
         */
    }

    /*
     * CORRECT: Expect 10-30% performance degradation
     */
    public class GoodExample_RealisticExpectations
    {
        /*
         * CORRECT THINKING:
         * "Backtest shows 30% annual return with 20% drawdown
         *  on tick data with realistic costs.
         *
         *  I expect live performance:
         *  - Conservative: 21% return (30% degradation)
         *  - Moderate: 24% return (20% degradation)
         *  - Optimistic: 27% return (10% degradation)
         *
         *  Max drawdown will likely be 1.5-2x backtest (30-40%)."
         *
         * WHY DEGRADATION HAPPENS:
         * - Slippage (1-3 pips per trade)
         * - Variable spreads (widen during news/volatility)
         * - Partial fills
         * - Requotes
         * - Network latency
         * - Different data feed
         * - Look-ahead bias in backtest
         * - Curve fitting despite best efforts
         *
         * ACCEPTANCE:
         * - 10-30% degradation is NORMAL
         * - If live is >50% worse, strategy failed
         * - Adjust expectations accordingly
         * - Focus on robustness, not perfection
         */
    }
}

/*
 * ========================================
 * SUMMARY: Top 10 Pitfalls to Avoid
 * ========================================
 *
 * 1. ✗ No risk management → ✓ Use RiskManager, limit drawdown
 * 2. ✗ Indicators in OnBar() → ✓ Initialize in OnStart()
 * 3. ✗ Modify collection while iterating → ✓ Use ToArray()
 * 4. ✗ Don't check for null → ✓ Always validate
 * 5. ✗ Optimize 10+ parameters → ✓ Limit to 2-3 max
 * 6. ✗ No error handling → ✓ Try-catch + retry logic
 * 7. ✗ Deploy without testing → ✓ Test for 3-4 months
 * 8. ✗ Use OnTick for everything → ✓ Use OnBar for most strategies
 * 9. ✗ Expect perfect backtest results → ✓ Accept 10-30% degradation
 * 10. ✗ Remove stop losses → ✓ ALWAYS use stop losses
 *
 * ========================================
 * Remember: The goal is NOT to make money fast.
 * The goal is to NOT LOSE money first, then slowly build profitability.
 * ========================================
 */
