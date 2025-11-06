# Advanced Multi-File Setup for cTrader

## ⚠️ CRITICAL: cTrader Reference System

**Important Discovery**: cTrader Automate has **limited cross-file reference support** between cBots.

## ✅ Working Solutions for Advanced Setup

I've identified **3 working approaches** for the advanced multi-file structure:

---

## 🎯 **Solution 1: Single Project with Multiple Source Files** (RECOMMENDED)

This is the proper way to use multi-file structure in cTrader.

### How It Works:
- All files go in the **SAME cBot project**
- Multiple `.cs` files in one cBot
- No external references needed
- Clean code organization

### Setup Steps:

#### 1. Create Main cBot
```
cTrader Automate → New cBot → Name: "RSI_BB_Strategy"
```

#### 2. Add Framework Files as Source Files

In cTrader, you can add multiple source files to a single cBot:

**Method A: Via cTrader Interface**
1. Right-click on your cBot project in the solution explorer
2. Select "Add" → "Existing Item" (or "Add Source File")
3. Add each framework file:
   - `RiskManager.cs`
   - `PositionSizer.cs`
   - `TradingLogger.cs`
   - `ErrorHandler.cs`

**Method B: Manual File Placement**
1. Navigate to cTrader's cBot folder:
   ```
   C:\Users\[YourName]\Documents\cAlgo\Sources\Robots\[YourBotName]\
   ```
2. Copy all framework `.cs` files into this folder
3. Restart cTrader or click "Refresh"
4. Files should appear in project

#### 3. Verify Structure
Your project should show:
```
RSI_BB_Strategy/
├── RSI_BB_Strategy.cs (main file)
├── RiskManager.cs
├── PositionSizer.cs
├── TradingLogger.cs
└── ErrorHandler.cs
```

#### 4. Build
- Click "Build"
- Should compile all files together
- Result: Single cBot with all framework code

### ✅ Advantages:
- Clean code organization
- Framework code reusable
- Professional structure
- Easy to maintain

### ❌ Limitations:
- Need to copy framework files for each strategy
- Changes to framework require updating each cBot

---

## 🎯 **Solution 2: Pre-Compilation with Visual Studio** (ADVANCED)

Compile framework as `.dll` library, reference in cTrader.

### Requirements:
- Visual Studio 2019+
- .NET Framework 4.0 SDK
- Understanding of C# projects

### Setup Steps:

#### 1. Create Class Library in Visual Studio
```csharp
File → New → Project → Class Library (.NET Framework 4.0)
Name: TradingBotFramework
```

#### 2. Add cTrader References
Add these DLL references to your project:
```
C:\Program Files\cTrader\cAlgo.API.dll
C:\Program Files\cTrader\cAlgo.dll
```

#### 3. Add Framework Code
- Copy all framework `.cs` files to project
- Ensure namespaces match: `TradingBot.Framework.*`

#### 4. Build DLL
```
Build → Build Solution
Result: TradingBotFramework.dll
```

#### 5. Reference in cTrader
1. Copy DLL to cTrader folder:
   ```
   C:\Users\[YourName]\Documents\cAlgo\Sources\Robots\[BotName]\bin\
   ```
2. In cTrader: Manage References → Browse → Select DLL
3. Build your strategy

### ✅ Advantages:
- True library system
- Framework compiled once
- Professional approach
- Easy framework updates

### ❌ Limitations:
- Requires Visual Studio
- More complex setup
- Need to manage DLL versions

---

## 🎯 **Solution 3: Indicator Pattern** (ALTERNATIVE)

Convert framework classes to cTrader Indicators (which CAN be referenced).

### Concept:
- cTrader Indicators can be referenced by cBots
- Framework code becomes "utility indicators"
- Not technically correct but WORKS

### Implementation:

**RiskManager as Indicator:**
```csharp
[Indicator(IsOverlay = true)]
public class RiskManagerIndicator : Indicator
{
    // Framework code here
    // Exposed via public methods
}
```

**Usage in Strategy:**
```csharp
private RiskManagerIndicator _riskManager;

protected override void OnStart()
{
    _riskManager = Indicators.GetIndicator<RiskManagerIndicator>();
}
```

### ✅ Advantages:
- Uses cTrader's native reference system
- No external tools needed
- Actually works in cTrader

### ❌ Limitations:
- Hacky solution
- Indicators aren't meant for this
- May have performance implications

---

## 🎯 **Recommended Approach for You**

Since you want the advanced setup, here's what I recommend:

### For Learning/Testing:
**Use Solution 1** (Single Project with Multiple Files)
- Easiest advanced setup
- Clean code organization
- No external tools needed

### For Production:
**Use Solution 2** (Visual Studio DLL)
- Professional approach
- Framework compiled once
- Easy to maintain

---

## 📋 **Step-by-Step: Solution 1 Implementation**

Let me create the exact files you need...

### File Structure:
```
Your cBot Project/
├── RSI_BollingerBands_MeanReversion.cs  (main strategy)
├── RiskManager.cs                        (framework)
├── PositionSizer.cs                      (framework)
├── TradingLogger.cs                      (framework)
└── ErrorHandler.cs                       (framework)
```

### Instructions:

1. **Create New cBot in cTrader**
   - Name: `RSI_BB_Advanced`

2. **Copy Main Strategy**
   - Replace default code with `Strategies/RSI_BollingerBands_MeanReversion.cs`

3. **Add Framework Files**
   - Method depends on cTrader version:

   **If cTrader has "Add File" option:**
   - Right-click project → Add → Existing Item
   - Select each framework file

   **If not:**
   - Navigate to: `C:\Users\[You]\Documents\cAlgo\Sources\Robots\RSI_BB_Advanced\`
   - Copy framework files there manually
   - Restart cTrader

4. **Build**
   - Click Build
   - Should see: "Build succeeded"

---

## ⚠️ **Known Issues & Solutions**

### Issue: "Namespace not found"

**Cause**: Files not in same project

**Solution**:
1. Verify all files in same folder
2. Check namespace matches: `TradingBot.Framework.*`
3. Restart cTrader

---

### Issue: "Type not accessible"

**Cause**: Class not `public`

**Solution**:
All framework classes should be:
```csharp
public class RiskManager  // ✓ public keyword
{
    // ...
}
```

---

### Issue: "Cannot add source file"

**Cause**: cTrader version doesn't support it

**Solution**:
1. Manually copy files to cBot folder
2. Or use standalone version
3. Or use Visual Studio approach

---

## 🧪 **Testing the Advanced Setup**

Once files are added:

### 1. Verify Files Visible
In cTrader project explorer, you should see all 5 files.

### 2. Build
Click Build - should succeed with 0 errors.

### 3. Check Output
```
Build succeeded
0 error(s), 0 warning(s)
Building project 'RSI_BB_Advanced'...
```

### 4. Run Backtest
Same as standalone version:
- Symbol: EURUSD
- Timeframe: h1
- Tick data
- Should work identically

---

## 📊 **Comparison: Standalone vs Advanced**

| Feature | Standalone | Advanced Multi-File |
|---------|-----------|---------------------|
| **Ease of Setup** | ✅ Very Easy | ⚠️ Moderate |
| **Code Organization** | ❌ Large single file | ✅ Clean separation |
| **Maintainability** | ❌ Harder to update | ✅ Easy to update framework |
| **Build Complexity** | ✅ Zero config | ⚠️ Requires file management |
| **Recommended For** | Beginners, testing | Experienced, production |

---

## ✅ **I'll Create the Exact Setup You Need**

Tell me:
1. **Do you have Visual Studio?** (for DLL approach)
2. **What cTrader version?** (affects which method works)
3. **Preferred approach:**
   - A) Single project with multiple files (easiest)
   - B) Visual Studio DLL (most professional)
   - C) Indicator pattern (alternative)

Based on your answer, I'll create the exact files and instructions you need.

---

## 🎯 **Quick Decision Guide**

**Choose Standalone if:**
- ❓ You want to test immediately
- ❓ You're new to cTrader
- ❓ You want guaranteed to work

**Choose Advanced Multi-File if:**
- ❓ You're comfortable with file management
- ❓ You want clean code organization
- ❓ You plan to build multiple strategies
- ❓ You want professional structure

**Choose Visual Studio DLL if:**
- ❓ You have Visual Studio
- ❓ You want true library system
- ❓ You're experienced developer
- ❓ You want production-ready setup

---

**Ready to proceed with advanced setup. Which approach do you prefer?**
