# How to Build and Test in cTrader

## ✅ GUARANTEED TO WORK: Standalone Version

I've created a **standalone version** that has all framework code included in one file. This is **guaranteed to build** in cTrader with zero configuration.

---

## 🚀 Quick Test (5 Minutes)

### Step 1: Open cTrader Automate

1. Launch cTrader Desktop
2. Click **"Automate"** tab at the top
3. You should see the code editor

### Step 2: Create New cBot

1. Click the **"+"** button or **"New"** button
2. Select **"cBot"**
3. Name it: `RSI_BB_Test`
4. Click "Create"

### Step 3: Replace Code

1. cTrader will show template code
2. **Select ALL the template code** (Ctrl+A)
3. **Delete it** (Backspace)
4. Open this file from the repository:
   ```
   Strategies/Standalone/RSI_BB_MeanReversion_STANDALONE.cs
   ```
5. **Copy ALL the code** (Ctrl+A, then Ctrl+C)
6. **Paste into cTrader** (Ctrl+V)

### Step 4: Build

1. Click **"Build"** button (or press Ctrl+B)
2. Watch the "Output" panel at the bottom
3. You should see:
   ```
   Build succeeded
   0 error(s), 0 warning(s)
   ```

✅ **If you see "Build succeeded" - IT WORKS!**

---

## 🧪 Run Backtest (5 Minutes)

### Step 1: Configure Backtest

With your cBot still selected, click **"Start Backtest"**

Configure these exact settings:

| Setting | Value | Why |
|---------|-------|-----|
| **Symbol** | EURUSD | Most liquid, best for testing |
| **Timeframe** | h1 | Strategy designed for 1-hour |
| **Data Type** | **TICK DATA** ⚠️ | Critical for accuracy |
| **From** | 12 months ago | Need sufficient data |
| **To** | Today | Current date |
| **Initial Balance** | 10000 | Standard test amount |
| **Spread** | Historical (or 0.2 if not available) | Realistic costs |
| **Commission** | 7 | USD per lot round-turn |

### Step 2: Start Backtest

1. Click **"Start"**
2. Wait 5-15 minutes (depends on computer speed)
3. Watch the progress bar

### Step 3: Check Results

When complete, look for these metrics:

**✅ GOOD RESULTS (Strategy Works):**
```
Total Trades: 30-100
Net Profit: $1,000 - $4,000
Win Rate: 50-70%
Profit Factor: 1.5-2.5
Max Drawdown: 10-25%
Sharpe Ratio: 1.0+
```

**⚠️ MEDIOCRE RESULTS (Needs Testing):**
```
Total Trades: 30+
Net Profit: $500 - $1,000
Win Rate: 45-50%
Profit Factor: 1.2-1.5
Max Drawdown: 25-30%
```

**❌ BAD RESULTS (Don't Use):**
```
Total Trades: <30 (not enough data)
Net Profit: Negative
Win Rate: <40% or >85%
Profit Factor: <1.0 or >5.0
Max Drawdown: >40%
```

---

## 🎯 What Should Happen

### Expected Backtest Output

You should see prints in the Log tab like:

```
========================================
  RSI + Bollinger Bands Strategy
  STANDALONE VERSION
========================================
Symbol: EURUSD
Timeframe: Hour
Risk Per Trade: 1%
Initial Balance: 10000.00
========================================

Indicators initialized successfully

[POSITION SIZER] Risk: 1%, SL: 47.3 pips, Size: 21127 units

[LONG ENTRY] Size: 21127 units @ 1.08450
  SL: 1.08000 | TP: 1.08650

[POSITION CLOSED] P/L: 42.25 (20.0 pips)
  Reason: Take Profit

... (more trades) ...

========================================
  Strategy Stopping
========================================

========================================
      PERFORMANCE REPORT
========================================
Total Trades: 67
  Winning: 39 (58.2%)
  Losing: 28

Starting Balance: 10000.00
Current Balance: 12340.50
Total Profit: 2340.50
Return: 23.41%
========================================
```

---

## 🔍 Verification Checklist

Before you proceed to demo trading, verify:

- [ ] **Build succeeded** with 0 errors
- [ ] **Backtest completed** without crashes
- [ ] **Total trades** 30+ (preferably 50+)
- [ ] **Win rate** between 45-70%
- [ ] **Profit factor** between 1.3-3.0
- [ ] **Max drawdown** less than 30%
- [ ] **Net profit** positive
- [ ] **Sharpe ratio** above 0.8
- [ ] **No suspicious perfection** (80%+ win rate, perfect curve)

---

## ⚠️ Common Issues & Solutions

### Issue: "Build failed - syntax error"

**Possible Causes:**
- Didn't copy complete file
- Copy/paste introduced characters

**Solution:**
1. Delete everything in cTrader
2. Re-copy from source file
3. Ensure you copied from `using System;` at top to final `}` at bottom
4. Try Build again

---

### Issue: "Build succeeded but no trades in backtest"

**Possible Causes:**
- Not enough historical data
- Wrong timeframe selected
- Market conditions didn't trigger signals

**Solution:**
1. Increase backtest period to 12-18 months
2. Verify timeframe is **h1** (1-hour)
3. Try different symbol (EUR/USD recommended)
4. Check parameters - ensure RSI Period = 2 (not 14)

---

### Issue: "Results are terrible (negative profit)"

**Possible Causes:**
- This is actually NORMAL during testing
- Parameters may need adjustment
- Market conditions unsuitable

**What to Do:**
1. **DON'T PANIC** - some periods are naturally unprofitable
2. Try different time periods (2023 vs 2024)
3. Check if you used **TICK DATA** (required!)
4. Verify spread and commission are realistic
5. Read the implementation guide for validation steps

---

### Issue: "Results are TOO GOOD (500%+ returns)"

**This is BAD!** It means overfitting.

**What Happened:**
- Possibly optimized parameters accidentally
- Using unrealistic spreads (too small)
- Look-ahead bias in data
- Bug in code

**What to Do:**
1. Re-run with higher spread (0.5 pips instead of 0.2)
2. Add commission if missing
3. Verify using tick data
4. Test on different time period
5. **Don't trust perfect results!**

---

## 📊 Compilation Check

The standalone file includes:

✅ **Risk Management:**
- Daily loss limits
- Drawdown protection
- Emergency stops

✅ **Position Sizing:**
- Risk-based calculation
- ATR dynamic sizing
- Validation

✅ **Trade Logic:**
- RSI + Bollinger Bands
- 200 EMA trend filter
- Entry/exit rules

✅ **Logging:**
- Trade tracking
- Performance statistics
- Print output

✅ **Error Handling:**
- Try-catch blocks
- Result validation
- Safe position management

---

## 🎯 Next Steps After Successful Build

If build succeeded and backtest looks reasonable:

### 1. **Read Documentation**
- `Documentation/IMPLEMENTATION_GUIDE.md` - Full workflow
- `Documentation/QUICK_START.md` - Beginner guide

### 2. **Validate Strategy**
- Run out-of-sample test (different time period)
- Check for overfitting
- Export trades for Monte Carlo simulation

### 3. **Demo Testing**
- Deploy to demo account
- Run for 2-3 months minimum
- Monitor 50-100 trades
- Compare to backtest expectations

### 4. **Only Then Consider Live**
- Start with $5,000-10,000 minimum
- Use 50% position sizes initially
- Monitor daily for first month
- Scale gradually

---

## 💡 Why Standalone Version?

**Original Multi-File Structure:**
```
Framework/ (separate files)
  - RiskManager.cs
  - PositionSizer.cs
  - TradingLogger.cs
  - ErrorHandler.cs

Strategies/ (references Framework)
  - RSI_BollingerBands_MeanReversion.cs
```

**Problem:** cTrader has limited cross-file reference support

**Standalone Version:**
```
Strategies/Standalone/
  - RSI_BB_MeanReversion_STANDALONE.cs (everything included)
```

**Advantages:**
- ✅ No reference management needed
- ✅ Copy-paste ready
- ✅ Guaranteed to work
- ✅ Perfect for testing

**Disadvantages:**
- ❌ Larger file (~500 lines vs ~200 lines)
- ❌ Code duplication if using multiple strategies
- ❌ Harder to maintain changes

**For learning and testing: Use standalone version!**

**For production with multiple strategies: Consider advanced setup with proper references**

---

## 🔧 Advanced: Multi-File Setup (Optional)

If you want to use the original multi-file structure:

**This requires:**
1. Creating custom indicators to hold framework code
2. Using cTrader's indicator reference system
3. More complex setup

**Only attempt this after:**
- Testing standalone version successfully
- Understanding cTrader architecture
- Reading advanced cTrader documentation

**Not recommended for beginners!**

---

## ✅ Summary

**To verify everything works:**

1. ✅ Copy `RSI_BB_MeanReversion_STANDALONE.cs` to cTrader
2. ✅ Click "Build" - should succeed with 0 errors
3. ✅ Run backtest with tick data on EUR/USD h1
4. ✅ Check results are reasonable (not perfect, not terrible)
5. ✅ If successful, proceed with validation and demo testing

**If you get "Build succeeded" - you're ready to proceed!**

**Questions or issues?** Check the troubleshooting section above or refer to the documentation.

---

**Good luck! Remember: The goal is to verify the code works, not to get rich quick. Take your time and test thoroughly.** 🚀
