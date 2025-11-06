# cTrader Algorithmic Trading Bot - Implementation Guide

## Table of Contents
1. [Quick Start](#quick-start)
2. [Project Structure](#project-structure)
3. [Strategy Selection](#strategy-selection)
4. [Setup and Configuration](#setup-and-configuration)
5. [Backtesting Workflow](#backtesting-workflow)
6. [Validation and Monte Carlo](#validation-and-monte-carlo)
7. [Demo Testing](#demo-testing)
8. [Going Live](#going-live)
9. [Troubleshooting](#troubleshooting)

---

## Quick Start

### Prerequisites
- cTrader Desktop installed (download from broker or ctrader.com)
- Basic understanding of C# (not required to use, but helpful for customization)
- Minimum $100-500 for demo/micro live testing
- Minimum $5,000-10,000 for serious live trading

### 5-Minute Setup

1. **Open cTrader Automate**
   - Launch cTrader Desktop
   - Click "Automate" tab at the top
   - Click "New" to create a new cBot

2. **Copy Strategy Code**
   - Choose one strategy from `/Strategies/` folder
   - For beginners: Start with `RSI_BollingerBands_MeanReversion.cs` (most conservative)
   - Copy the entire file contents
   - Paste into cTrader code editor

3. **Add Framework Files**
   - Copy all files from `/Framework/` directory
   - In cTrader, click "Manage References" → "Add Source"
   - Add each framework file (RiskManager.cs, PositionSizer.cs, etc.)

4. **Build and Test**
   - Click "Build" button (or Ctrl+B)
   - Fix any compilation errors
   - Click "Start Backtest" to test on historical data

---

## Project Structure

```
Trade_bot/
├── Framework/              # Core reusable components
│   ├── RiskManagement/
│   │   ├── RiskManager.cs         # Drawdown limits, daily/weekly loss tracking
│   │   └── PositionSizer.cs       # ATR-based, Kelly Criterion sizing
│   ├── Logging/
│   │   └── TradingLogger.cs       # Performance tracking, CSV export
│   └── ErrorHandling/
│       └── ErrorHandler.cs         # Retry logic, circuit breakers
│
├── Strategies/            # Ready-to-use trading strategies
│   ├── RSI_BollingerBands_MeanReversion.cs    # Conservative, 15-30% annual
│   ├── EMA_Trend_Following_ADX.cs             # Moderate, 20-40% annual
│   └── London_Breakout_Session.cs             # Aggressive, 30-50% annual
│
├── Utilities/            # Tools for analysis
│   ├── Backtesting/
│   │   └── BacktestValidator.cs   # Overfitting detection, validation
│   └── MonteCarlo/
│       └── MonteCarloExporter.cs  # Risk of ruin, simulation export
│
├── Examples/             # Educational code samples
│   └── (Coming soon)
│
└── Documentation/        # Guides and references
    ├── IMPLEMENTATION_GUIDE.md    # This file
    ├── STRATEGY_COMPARISON.md     # Which strategy to choose
    └── QUICK_START.md             # Absolute beginner guide
```

---

## Strategy Selection

### Which Strategy Should You Use?

| Strategy | Risk Level | Expected Return | Max Drawdown | Win Rate | Best For |
|----------|------------|-----------------|--------------|----------|----------|
| **RSI + Bollinger Bands** | Low | 15-30% | 10-20% | 55-65% | Conservative traders, beginners |
| **EMA Trend Following** | Moderate | 20-40% | 15-25% | 40-50% | Trend traders, intermediate |
| **London Breakout** | High | 30-50% | 20-30% | 60-70% | Aggressive traders, experienced |

### Detailed Strategy Comparison

#### 1. RSI + Bollinger Bands Mean Reversion
**File:** `RSI_BollingerBands_MeanReversion.cs`

**When to Use:**
- You want consistent, steady returns
- You can tolerate 10-20% drawdowns
- You prefer ranging markets
- You're new to algorithmic trading

**Best Pairs:** EUR/USD, USD/CAD, EUR/GBP
**Best Timeframe:** 1-hour
**Trading Frequency:** 5-10 trades per week

**Key Parameters:**
- `RiskPercent`: 1.0% (conservative)
- `RsiPeriod`: 2 (NOT default 14 - this is important!)
- `RsiOversold`: 30
- `RsiOverbought`: 70
- `BbPeriod`: 20
- `BbStdDev`: 2.0
- `TrendEmaPeriod`: 200 (critical trend filter)

**Pros:**
- Higher win rate (55-65%)
- Smaller drawdowns
- Easier psychologically
- Works in ranging markets

**Cons:**
- Lower returns than trend following
- Fails in strong trending markets
- Requires 200 EMA filter (essential)

---

#### 2. EMA Trend Following with ADX
**File:** `EMA_Trend_Following_ADX.cs`

**When to Use:**
- You want to catch big trending moves
- You can handle 15-25% drawdowns
- You're comfortable with 40-50% win rate
- You understand trend trading psychology

**Best Pairs:** GBP/USD, EUR/USD, AUD/USD
**Best Timeframe:** 1-hour (with daily confirmation)
**Trading Frequency:** 3-7 trades per week

**Key Parameters:**
- `RiskPercent`: 1.5% (moderate)
- `FastEmaPeriod`: 21
- `SlowEmaPeriod`: 50
- `TrendEmaPeriod`: 200
- `AdxPeriod`: 14
- `AdxMinimum`: 25 (only trade strong trends)
- `RiskRewardRatio`: 2.0 (minimum 1:2)

**Pros:**
- Higher potential returns (20-40%)
- Better risk-reward (1:2 or 1:3)
- Catches major market moves
- Multi-timeframe confirmation

**Cons:**
- Lower win rate (40-50%)
- Larger drawdowns in ranging markets
- Psychologically harder (more losses)
- Requires patience

---

#### 3. London Breakout Session
**File:** `London_Breakout_Session.cs`

**When to Use:**
- You want aggressive returns
- You can handle 20-30% drawdowns
- You can monitor trades during London session
- You have experience with breakout strategies

**Best Pairs:** EUR/USD, GBP/USD
**Best Timeframe:** 15-minute
**Trading Frequency:** 2-5 trades per day (during London session)

**Key Parameters:**
- `RiskPercent`: 1.5% (moderate-aggressive)
- `AsianSessionStartHour`: 0 (UTC)
- `AsianSessionEndHour`: 7 (UTC)
- `LondonSessionStartHour`: 8 (UTC)
- `MinRangeSizePips`: 15
- `MaxRangeSizePips`: 80
- `RiskRewardRatio`: 1.5

**Pros:**
- High win rate (60-70%)
- Exploits high-liquidity session
- Clear entry/exit rules
- Multiple trades daily

**Cons:**
- Time-specific (requires London session)
- Higher frequency = more stress
- False breakouts during low volatility
- Requires monitoring

---

## Setup and Configuration

### Step 1: Install Strategy in cTrader

1. **Copy Framework Files**
   ```
   Framework/
     RiskManagement/RiskManager.cs
     RiskManagement/PositionSizer.cs
     Logging/TradingLogger.cs
     ErrorHandling/ErrorHandler.cs
   ```

2. **In cTrader Automate:**
   - Create new cBot
   - Click "Manage References"
   - Add each framework file as source reference
   - Ensure all namespaces match: `TradingBot.Framework.*`

3. **Copy Strategy File**
   - Choose one strategy from `/Strategies/`
   - Copy entire contents
   - Paste into main cBot code window
   - Ensure framework references are correct

4. **Build**
   - Press Ctrl+B or click "Build"
   - Fix any compilation errors
   - Successful build = ready to backtest

### Step 2: Configure Parameters

#### Risk Parameters (Critical - DO NOT Skip!)

```csharp
// Risk per trade - NEVER exceed 2%
[Parameter("Risk Per Trade (%)", DefaultValue = 1.0)]
public double RiskPercent { get; set; }

// Daily loss limit - Emergency stop
[Parameter("Max Daily Loss %", DefaultValue = 2.0)]
public double MaxDailyLossPercent { get; set; }

// Weekly loss limit
[Parameter("Max Weekly Loss %", DefaultValue = 5.0)]
public double MaxWeeklyLossPercent { get; set; }

// Maximum drawdown from peak
[Parameter("Max Drawdown %", DefaultValue = 20.0)]
public double MaxDrawdownPercent { get; set; }

// Maximum open positions simultaneously
[Parameter("Max Concurrent Positions", DefaultValue = 5)]
public int MaxConcurrentPositions { get; set; }
```

**Conservative Settings:**
- Risk per trade: 0.5-1.0%
- Max daily loss: 2.0%
- Max weekly loss: 5.0%
- Max drawdown: 15-20%
- Max positions: 3-5

**Aggressive Settings:**
- Risk per trade: 1.5-2.0%
- Max daily loss: 3.0%
- Max weekly loss: 7.0%
- Max drawdown: 25-30%
- Max positions: 5-10

---

## Backtesting Workflow

### Critical Backtesting Settings

#### ALWAYS Use These Settings:

1. **Data Type: TICK DATA** ✓
   - NOT 1-minute bars
   - NOT M1 approximation
   - Use actual tick-by-tick data

2. **Spread: Historical or Conservative**
   - Historical spread (recommended)
   - OR Fixed spread + 2x multiplier for safety
   - Example: EUR/USD = 0.1 pips → Use 0.2 pips

3. **Commission: Match Your Broker**
   - ECN broker: $3-7 per lot round-turn
   - Market maker: Usually included in spread

4. **Time Period: 6-12 Months Minimum**
   - Absolute minimum: 6 months
   - Recommended: 12-24 months
   - Include different market conditions

5. **Initial Balance: Match Live Account**
   - Planning $10K live? Use $10K backtest
   - Position sizing scales with account size

### Step-by-Step Backtesting Process

#### Step 1: Initial Backtest

```
1. Open cTrader Automate
2. Select your strategy cBot
3. Click "Start Backtest"
4. Configure settings:
   - Symbol: EUR/USD (or your target pair)
   - Timeframe: 1-hour (or strategy-specific)
   - Data Type: TICK DATA ✓
   - From: 12 months ago
   - To: Today
   - Initial Balance: $10,000
   - Spread: Historical
   - Commission: $7 per lot
5. Click "Start"
6. Wait for completion (may take 10-30 minutes)
```

#### Step 2: Analyze Results

**Minimum Acceptable Metrics:**
- Total Trades: 30+ (100+ preferred)
- Win Rate: 40-70% (depends on strategy)
- Profit Factor: 1.5-2.5
- Sharpe Ratio: 1.0+ (1.5+ preferred)
- Max Drawdown: <30%
- Return: Positive and realistic

**Red Flags:**
- Win rate > 80% → Likely overfit
- Profit factor > 5.0 → Likely overfit
- Max DD < 5% → Too perfect, suspicious
- Perfect equity curve → Overfitting
- Return > 500% → Curve-fitted

#### Step 3: Validate with Out-of-Sample

**Manual Walk-Forward Process:**

```
1. In-Sample Period: First 80% of data
   - Example: Jan 2023 - Aug 2023 (8 months)
   - Optimize parameters on this data
   - Record best parameters

2. Out-of-Sample Period: Last 20% of data
   - Example: Sep 2023 - Oct 2023 (2 months)
   - Test with optimized parameters
   - DO NOT re-optimize

3. Compare Results:
   - Sharpe ratio difference <30%? ✓ Good
   - Win rate difference <15%? ✓ Good
   - Profit factor difference <40%? ✓ Good
   - Return still positive? ✓ Good
```

**If Out-of-Sample Fails:**
- Degradation >50% → Overfit, start over
- Negative returns → Strategy doesn't work
- Max DD 2x in-sample → Reduce position size

---

## Validation and Monte Carlo

### Step 1: Export Trade Data

After backtesting, export trade log:

```csharp
// In your strategy's OnStop() method
string csvData = _logger.ExportToCSV();
Print("\nTrade Log CSV:");
Print(csvData);
```

Copy the CSV output from cTrader logs.

### Step 2: Run Backtest Validator

Use the `BacktestValidator` utility to check for issues:

```csharp
var results = new BacktestResults
{
    TotalTrades = 87,
    WinningTrades = 52,
    LosingTrades = 35,
    WinRate = 0.598,
    ProfitFactor = 1.82,
    SharpeRatio = 1.45,
    MaxDrawdownPercent = 18.5,
    ReturnPercent = 23.7,
    AvgWinLossRatio = 1.35,
    MaxConsecutiveWins = 6,
    MaxConsecutiveLosses = 4,
    BacktestDurationDays = 365
};

var settings = new BacktestSettings
{
    IncludedCommissions = true,
    IncludedSpread = true,
    UsedTickData = true,
    UsedVariableSpread = true
};

var validation = BacktestValidator.ValidateBacktest(results, settings);
Print(validation.GetReport());
```

**Sample Output:**
```
=== BACKTEST VALIDATION REPORT ===

WARNINGS:
  [WARNING] Trade count: 87 (100+ recommended for robustness)

✓ All other validation checks passed!

VERDICT: PASSED
================================
```

### Step 3: Monte Carlo Simulation

**Option A: Python (Recommended)**

1. Use `MonteCarloExporter.ExportToPython()` to generate script
2. Save as `monte_carlo.py`
3. Install requirements: `pip install numpy matplotlib scipy`
4. Run: `python monte_carlo.py`
5. Analyze output statistics and charts

**Option B: Build Alpha Online (Easiest)**

1. Use `MonteCarloExporter.ExportForBuildAlpha()`
2. Visit: https://buildalpha.com/monte-carlo-simulator/
3. Paste returns data
4. Set simulations: 1000+
5. Click "Run"
6. Review results

**Key Monte Carlo Metrics:**

- **95th Percentile Drawdown**: Expect this in live trading
  - <30%: Excellent ✓
  - 30-50%: Acceptable ⚠
  - >50%: Too risky ✗

- **Probability of 50%+ Drawdown**:
  - <2%: Acceptable ✓
  - 2-5%: Caution ⚠
  - >5%: Too risky ✗

- **Mean Return**: Should be close to backtest
  - Within 20%: Good ✓
  - 20-40% difference: Acceptable ⚠
  - >40% difference: Problem ✗

---

## Demo Testing

### Minimum Demo Requirements

- **Duration**: 2-3 months minimum
- **Trades**: 50-100 minimum
- **Account Size**: Match intended live account
- **Broker**: Same broker as planned live
- **Monitoring**: Daily checks

### Demo Testing Checklist

**Week 1-2: Technical Validation**
- [ ] Bot starts without errors
- [ ] Trades execute as expected
- [ ] Stop losses work correctly
- [ ] Take profits trigger properly
- [ ] No connection issues
- [ ] Logging functions correctly

**Week 3-4: Performance Validation**
- [ ] Win rate within 10% of backtest
- [ ] Average win/loss similar to backtest
- [ ] Slippage is acceptable (0.5-2 pips)
- [ ] Commission matches expectations
- [ ] No unexpected behavior

**Month 2: Psychological Validation**
- [ ] Comfortable watching drawdowns
- [ ] No urge to override bot
- [ ] Can handle consecutive losses
- [ ] Confident in strategy logic
- [ ] Trust in risk management

**Month 3: Decision Point**
- [ ] Performance within 30% of backtest? → Proceed
- [ ] No major technical issues? → Proceed
- [ ] Emotionally comfortable? → Proceed
- [ ] Monte Carlo expectations met? → Proceed

**If ANY answer is NO:** Continue demo for another month or reconsider strategy.

---

## Going Live

### Pre-Live Checklist

- [ ] Minimum 2 months demo testing complete
- [ ] 50+ demo trades executed successfully
- [ ] Performance within 30% of backtest expectations
- [ ] No technical issues on demo
- [ ] Monte Carlo analysis shows acceptable risk
- [ ] Broker verified and funded
- [ ] VPS setup (optional but recommended)
- [ ] Emergency contact information ready
- [ ] Trade journal prepared
- [ ] Psychological preparation complete

### Live Deployment Steps

#### Step 1: Start Small

- **Initial Capital**: $5,000-10,000 minimum
- **Position Sizes**: 50% of intended size for first month
- **Pairs**: Start with most liquid (EUR/USD only)
- **Monitoring**: Daily minimum (multiple times first week)

#### Step 2: Gradual Scaling

**Month 1:**
- 50% position sizes
- 1 pair only
- Daily monitoring
- Keep detailed journal

**Month 2:**
- 75% position sizes if performing well
- Add second pair if desired
- Monitor 3x weekly
- Review statistics weekly

**Month 3:**
- 100% position sizes if confidence high
- Multiple pairs acceptable
- Monitor weekly minimum
- Monthly performance review

**Month 4+:**
- Full deployment
- Regular monitoring
- Continuous optimization
- Quarterly strategy review

#### Step 3: Ongoing Management

**Daily (First Month):**
- Check open positions
- Verify no errors
- Monitor daily P&L
- Check risk limits

**Weekly:**
- Review performance metrics
- Compare to expectations
- Check drawdown levels
- Verify risk management working

**Monthly:**
- Full performance analysis
- Compare to backtest/demo
- Risk of ruin assessment
- Strategy adjustment decision

**Quarterly:**
- Walk-forward reoptimization
- Market regime analysis
- Consider strategy changes
- Capital allocation review

### When to Stop Trading

**Immediate Stop Triggers:**
- Daily loss limit hit (automatic)
- Weekly loss limit hit (automatic)
- Max drawdown limit hit (automatic)
- Technical issues (connection, execution)
- Major news event affecting strategy
- Personal emergency requiring attention

**Review and Pause Triggers:**
- Performance >50% below expectations for 2+ months
- Max drawdown exceeds Monte Carlo 95th percentile
- Win rate drops >15% from backtest
- 10+ consecutive losses
- Significant market regime change
- Loss of confidence in strategy

**Strategy Retirement Triggers:**
- Consistent underperformance for 6+ months
- Market structure permanent change
- Better opportunity identified
- Risk tolerance changed
- Strategy logic invalidated

---

## Troubleshooting

### Common Issues and Solutions

#### Issue: "Build Failed - Namespace not found"

**Solution:**
```csharp
// Ensure all using statements are present:
using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using TradingBot.Framework.RiskManagement;
using TradingBot.Framework.Logging;
using TradingBot.Framework.ErrorHandling;
```

Also verify framework files are added as references in cTrader.

---

#### Issue: "Bot stops trading after a few trades"

**Possible Causes:**
1. Risk limits triggered (check logs for "Emergency Stop")
2. Circuit breaker tripped (10+ consecutive errors)
3. Daily/weekly loss limit reached

**Solution:**
- Check cTrader logs for specific error
- Review `RiskManager` statistics
- Verify risk parameters are reasonable
- Check for connection issues

---

#### Issue: "Performance much worse than backtest"

**Common Reasons:**
1. Used 1-minute bars instead of tick data
2. Didn't include realistic spread/commission
3. Overfit parameters
4. Different data feed (demo vs live)
5. Broker restrictions (anti-scalping)

**Solution:**
- Re-run backtest with tick data + conservative costs
- Use walk-forward optimization
- Test on different time periods
- Verify broker conditions match backtest assumptions

---

#### Issue: "Position sizes too small/large"

**Solution:**
```csharp
// Check these in PositionSizer:
double positionSize = _positionSizer.CalculatePositionSizeByRisk(
    RiskPercent,      // Should be 0.5-2.0%
    stopLossPips      // Verify this is reasonable (20-100 pips)
);

// Validate:
if (!_positionSizer.ValidatePositionSize(positionSize))
{
    // Will print error if outside limits
}
```

---

#### Issue: "No trades executing"

**Checklist:**
- [ ] Strategy logic correct (check entry conditions)
- [ ] Risk limits not breached (check `_riskManager.CanOpenNewPosition()`)
- [ ] Market open (check trading hours for pair)
- [ ] Sufficient margin (check account equity)
- [ ] No position limit reached (check max concurrent)

---

## Next Steps

1. **Choose Your Strategy** → See [Strategy Comparison](#strategy-selection)
2. **Setup in cTrader** → Follow [Setup and Configuration](#setup-and-configuration)
3. **Run Backtests** → Use [Backtesting Workflow](#backtesting-workflow)
4. **Validate Results** → Apply [Validation and Monte Carlo](#validation-and-monte-carlo)
5. **Demo Test** → Complete [Demo Testing](#demo-testing)
6. **Go Live** → Follow [Going Live](#going-live)

---

## Additional Resources

- **cTrader API Documentation**: https://help.ctrader.com/ctrader-automate/
- **Build Alpha MC Simulator**: https://buildalpha.com/monte-carlo-simulator/
- **Risk of Ruin Calculator**: https://www.myfxbook.com/forex-calculators/risk-of-ruin

---

**Remember**: 85-90% of retail algorithmic traders lose money. Success requires discipline, realistic expectations, proper risk management, and 3-5 years to achieve consistency. Start small, test thoroughly, and never risk more than you can afford to lose.

**Good luck, and trade responsibly!**
