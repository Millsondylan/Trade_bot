# Quick Start Guide - Your First cTrader Trading Bot

## For Absolute Beginners

This guide will help you deploy your first algorithmic trading bot in cTrader in **under 30 minutes**, even if you have zero coding experience.

---

## What You Need

- ✓ **cTrader Desktop** installed (free download from your broker or ctrader.com)
- ✓ **Demo account** with any cTrader broker (free, no deposit required)
- ✓ **30 minutes** of your time
- ✓ **This repository** downloaded

**No coding experience required!** Just copy and paste.

---

## Step 1: Download cTrader (5 minutes)

### If you don't have cTrader yet:

1. Visit https://ctrader.com/
2. Click "Download cTrader Desktop"
3. Install the application
4. Open cTrader and create/connect demo account

### Recommended Demo Account Brokers:
- **IC Markets**: https://www.icmarkets.com/ (popular, good spreads)
- **Pepperstone**: https://pepperstone.com/ (excellent execution)
- **FxPro**: https://www.fxpro.com/ (reliable)

**Important**: Use DEMO account first. Never start with real money.

---

## Step 2: Choose Your Strategy (2 minutes)

We have 3 strategies ready to use. Choose based on your risk tolerance:

### 🟢 Conservative (Recommended for Beginners)
**Strategy**: RSI + Bollinger Bands Mean Reversion
- Expected return: 15-30% per year
- Expected max drawdown: 10-20%
- File: `Strategies/RSI_BollingerBands_MeanReversion.cs`

### 🟡 Moderate
**Strategy**: EMA Trend Following with ADX
- Expected return: 20-40% per year
- Expected max drawdown: 15-25%
- File: `Strategies/EMA_Trend_Following_ADX.cs`

### 🔴 Aggressive
**Strategy**: London Breakout Session
- Expected return: 30-50% per year
- Expected max drawdown: 20-30%
- File: `Strategies/London_Breakout_Session.cs`

**For your first bot, choose the CONSERVATIVE strategy** (RSI + Bollinger Bands).

---

## Step 3: Install Framework Files (10 minutes)

The framework files provide risk management, logging, and error handling. You need to install these first.

### 3.1: Create New cBot

1. Open cTrader Desktop
2. Click **"Automate"** tab at the top
3. Click **"New"** button (top left)
4. Select **"cBot"**
5. Name it: `TradingFramework`
6. Click **"Create"**

### 3.2: Copy Framework Files

You need to copy 4 framework files as "references":

1. **RiskManager.cs**:
   - Open `Framework/RiskManagement/RiskManager.cs` from this repository
   - Copy ALL the code (Ctrl+A, then Ctrl+C)
   - In cTrader, click **"+"** next to "References" tab
   - Select **"Add Source"**
   - Paste the code
   - Click "OK"

2. **PositionSizer.cs**:
   - Open `Framework/RiskManagement/PositionSizer.cs`
   - Copy ALL the code
   - Click **"+"** → **"Add Source"**
   - Paste the code
   - Click "OK"

3. **TradingLogger.cs**:
   - Open `Framework/Logging/TradingLogger.cs`
   - Copy ALL the code
   - Click **"+"** → **"Add Source"**
   - Paste the code
   - Click "OK"

4. **ErrorHandler.cs**:
   - Open `Framework/ErrorHandling/ErrorHandler.cs`
   - Copy ALL the code
   - Click **"+"** → **"Add Source"**
   - Paste the code
   - Click "OK"

### 3.3: Verify Framework Installation

You should now see 4 files under "References" in cTrader:
- RiskManager.cs
- PositionSizer.cs
- TradingLogger.cs
- ErrorHandler.cs

---

## Step 4: Install Your Strategy (5 minutes)

Now let's install the actual trading strategy.

### 4.1: Create Strategy cBot

1. In cTrader Automate, click **"New"** again
2. Select **"cBot"**
3. Name it based on your chosen strategy:
   - Conservative: `RSI_BB_Strategy`
   - Moderate: `EMA_Trend_Strategy`
   - Aggressive: `London_Breakout_Strategy`
4. Click **"Create"**

### 4.2: Copy Strategy Code

1. Open your chosen strategy file:
   - Conservative: `Strategies/RSI_BollingerBands_MeanReversion.cs`
   - Moderate: `Strategies/EMA_Trend_Following_ADX.cs`
   - Aggressive: `Strategies/London_Breakout_Session.cs`

2. **Select ALL the code** (Ctrl+A)

3. **Copy it** (Ctrl+C)

4. In cTrader code editor, **DELETE all existing code** (usually template code)

5. **Paste** your strategy code (Ctrl+V)

### 4.3: Add Framework References

Your strategy needs to access the framework files:

1. Click **"Manage References"** button (usually at top)
2. Click **"Add Reference"**
3. Select **"RiskManager.cs"**
4. Click "OK"
5. Repeat for:
   - PositionSizer.cs
   - TradingLogger.cs
   - ErrorHandler.cs

### 4.4: Build the Strategy

1. Click **"Build"** button (or press Ctrl+B)
2. Wait for compilation
3. Check the "Output" tab at bottom
4. You should see: **"Build succeeded"** ✓

**If you see errors**: Make sure you copied ALL framework files and added them as references.

---

## Step 5: Run Your First Backtest (5 minutes)

Time to see if your strategy makes money on historical data!

### 5.1: Configure Backtest Settings

1. Click **"Start Backtest"** button
2. Configure these settings:

**Symbol**: EUR/USD
**Timeframe**:
   - Conservative/Moderate: h1 (1-hour)
   - Aggressive: m15 (15-minute)

**Date Range**:
   - From: 1 year ago
   - To: Today

**Initial Balance**: 10000 (EUR/USD uses USD)

**Data Type**: **TICK DATA** ⚠ (Very important!)

**Spread**: Historical

**Commission**: 7 (USD per lot round-turn)

### 5.2: Start Backtest

1. Click **"Start"** button
2. Wait for backtest to complete (may take 5-15 minutes)
3. cTrader will show progress bar

### 5.3: Review Results

When complete, cTrader shows:
- **Net Profit**: Total profit/loss
- **Return %**: Percentage return on initial balance
- **Win Rate %**: Percentage of winning trades
- **Max Drawdown**: Worst peak-to-valley decline
- **Sharpe Ratio**: Risk-adjusted returns

**Good Results Look Like:**
```
Net Profit: $2,000 - $5,000  ✓
Return %: 20% - 50%  ✓
Win Rate: 45% - 70%  ✓
Max Drawdown: 10% - 25%  ✓
Sharpe Ratio: 1.0+  ✓
```

**Bad Results Look Like:**
```
Net Profit: Negative  ✗
Return %: <0%  ✗
Win Rate: <40% or >85%  ✗
Max Drawdown: >50%  ✗
Sharpe Ratio: <0  ✗
```

---

## Step 6: Adjust Parameters (Optional, 5 minutes)

If backtest results are poor, you can adjust strategy parameters:

### For Conservative Strategy (RSI + Bollinger Bands):

```csharp
[Parameter("Risk Per Trade (%)", DefaultValue = 1.0)]
public double RiskPercent { get; set; }  // Try: 0.5 - 1.5

[Parameter("RSI Period", DefaultValue = 2)]
public int RsiPeriod { get; set; }  // Keep at 2! (NOT 14)

[Parameter("RSI Oversold", DefaultValue = 30)]
public double RsiOversold { get; set; }  // Try: 25-35

[Parameter("RSI Overbought", DefaultValue = 70)]
public double RsiOverbought { get; set; }  // Try: 65-75
```

**How to Adjust:**
1. Find the parameter in the code (usually at top)
2. Change `DefaultValue = X` to new value
3. Click "Build"
4. Run backtest again

**Optimization Tips:**
- Change ONE parameter at a time
- Don't over-optimize (chasing perfect results = overfitting)
- If changing one thing drastically changes results, strategy is fragile

---

## Step 7: Deploy to Demo Account (3 minutes)

If backtest results are good (positive return, acceptable drawdown), test on demo:

### 7.1: Configure Parameters

1. In cTrader Automate, select your strategy cBot
2. Click **"New Instance"** (not backtest)
3. Configure:
   - **Symbol**: EUR/USD
   - **Timeframe**: h1 (or m15 for London Breakout)
   - **Risk Per Trade**: 1.0% (or less)
   - **Max Daily Loss**: 2.0%
   - **Max Weekly Loss**: 5.0%
   - **Max Drawdown**: 20.0%

### 7.2: Start Bot

1. Click **"Start"** button
2. Bot will show as "Running" with green indicator
3. It will automatically trade when signals occur

### 7.3: Monitor Bot

- **First Day**: Check every few hours
- **First Week**: Check 2-3 times daily
- **After Week 1**: Check daily

**Look for:**
- Trades executing
- Stop losses working
- No error messages
- Reasonable P&L

---

## Step 8: Understand What Happens Next

### Demo Testing Timeline

**Week 1-2**: Technical validation
- Ensure bot works without errors
- Verify trades execute as expected
- Check risk management functioning

**Week 3-4**: Performance validation
- Compare to backtest expectations
- Acceptable if within 30% of backtest results
- Watch for slippage and execution quality

**Month 2-3**: Psychological validation
- Can you handle watching losses?
- Do you trust the strategy?
- Are you comfortable with drawdowns?

**After 2-3 Months**: Decision point
- If demo successful (50-100 trades, performance acceptable): Consider live
- If demo poor (>50% worse than backtest): Back to drawing board
- If uncertain: Continue demo longer

### Going Live

**ONLY after:**
- ✓ 2-3 months successful demo
- ✓ 50+ trades on demo
- ✓ Performance within 30% of backtest
- ✓ No technical issues
- ✓ Comfortable with drawdowns
- ✓ $5,000-10,000 minimum capital

**Start Live:**
- 50% position sizes first month
- 1 currency pair only
- Daily monitoring
- Scale gradually if successful

---

## Common Beginner Questions

### Q: How much money do I need?

**For Demo**: $0 (demo account is free, virtual money)

**For Live**:
- Minimum: $500-1,000 (micro lots)
- Recommended: $5,000-10,000 (mini lots)
- Serious trading: $10,000-50,000 (standard lots)

Lower capital = lower absolute returns but same percentage returns.

---

### Q: How often will it trade?

**Conservative (RSI + BB)**: 5-10 trades per week
**Moderate (EMA Trend)**: 3-7 trades per week
**Aggressive (London Breakout)**: 2-5 trades per day (during London session)

The bot trades automatically when signals occur.

---

### Q: Do I need to watch it constantly?

**No**, but:
- First week: Check 2-3 times daily
- After that: Check daily
- After month: Check 2-3 times weekly

Set up alerts in cTrader for:
- Daily loss limit hit
- Drawdown limit hit
- Error messages

---

### Q: What if it loses money?

**Expected**: ALL strategies have losing trades and losing periods.

**Normal**:
- 40-60% of individual trades lose
- 1-2 losing weeks per month
- 1-2 losing months per year
- 10-30% drawdowns

**Not Normal**:
- Performance >50% worse than backtest
- Max drawdown >2x backtest
- Consistent losses for 3+ months
- Win rate drops >20% from backtest

If "Not Normal", stop and investigate.

---

### Q: Can I tweak it during trading?

**NO!** ⚠

**Why**: You'll likely make it worse. Common mistakes:
- Closing winning trades early
- Overriding stop losses
- Changing parameters during drawdown
- Turning off bot when losing

**Discipline is CRITICAL**. Let the bot run as designed.

**Exception**: Only stop for:
- Major technical errors
- Risk limits breached
- Broker issues
- Emergency situations

---

### Q: How long until I'm profitable?

**Reality Check**:
- 85-90% of retail algo traders LOSE money
- Most successful traders take 3-5 years to achieve consistency
- You'll likely lose money for 6-18 months while learning
- Budget $2,000-5,000 as "tuition" (expected losses)

**Timeline for 10-15% who succeed**:
- Months 1-6: Learning, small losses expected
- Months 7-18: Development, break-even to small profits
- Months 19-36: Validation, consistent small profits
- Years 3-5: Scaling, meaningful profits

---

### Q: What are realistic returns?

**Conservative approach**: 15-30% annual
**Moderate approach**: 20-40% annual
**Aggressive approach**: 30-50% annual

**On $10,000 account**:
- Conservative: $1,500-3,000 per year
- Moderate: $2,000-4,000 per year
- Aggressive: $3,000-5,000 per year

**Warning**: Anyone claiming 100%+ annual returns consistently is either:
- Using extreme risk (likely to blow up)
- Curve-fitted backtest (won't work live)
- Lying

---

### Q: Should I use real money now?

**NO!** Not yet.

**Sequence**:
1. Backtest (ensure positive results) ← You are here
2. Demo test 2-3 months (ensure works live)
3. Micro live 1 month ($500-1,000, tiny positions)
4. Small live 2-3 months ($5,000, 50% positions)
5. Full live (after 6+ months total testing)

**Skipping steps = losing money faster.**

---

## Troubleshooting

### "Build failed" error

**Cause**: Framework files not properly referenced

**Solution**:
1. Click "Manage References"
2. Ensure all 4 framework files are listed
3. If not, re-add them using "Add Reference"
4. Click "Build" again

---

### "No trades executing on demo"

**Possible causes**:
1. Strategy waiting for signal (be patient)
2. Risk limits already hit (check logs)
3. Market closed (check trading hours)
4. Wrong timeframe selected

**Solution**:
- Check cTrader "Log" tab for messages
- Verify correct symbol and timeframe
- Wait at least 24-48 hours for first trade

---

### "Results much worse than backtest"

**Common reasons**:
1. Didn't use tick data in backtest
2. Demo has different spread/commission
3. Overfit parameters
4. Normal 10-30% degradation

**Solution**:
- Re-run backtest with realistic costs
- Give it 50+ trades before judging
- Compare average trade metrics, not total profit

---

## Next Steps

1. ✅ Run backtest with conservative strategy
2. ✅ Analyze results
3. ✅ If positive, deploy to demo
4. ✅ Monitor for 2-3 months
5. ✅ Read full [Implementation Guide](IMPLEMENTATION_GUIDE.md)
6. ✅ Learn about [Monte Carlo validation](IMPLEMENTATION_GUIDE.md#validation-and-monte-carlo)
7. ✅ After successful demo, consider micro live

---

## Important Reminders

🔴 **NEVER**:
- Start with real money without demo testing
- Risk more than 1-2% per trade
- Override the bot during drawdowns
- Expect to get rich quick
- Trade without stop losses
- Ignore risk management

🟢 **ALWAYS**:
- Test thoroughly on demo first
- Use proper risk management (1% per trade)
- Be patient (takes 2-3 years)
- Monitor your bot regularly
- Keep learning and improving
- Accept that losses are normal

---

**You're now ready to deploy your first algorithmic trading bot!**

**Good luck, and remember**: This is a marathon, not a sprint. Most fail because they rush. Take your time, test thoroughly, and never risk more than you can afford to lose.

**Questions?** Review the detailed [Implementation Guide](IMPLEMENTATION_GUIDE.md) or check cTrader forums.
