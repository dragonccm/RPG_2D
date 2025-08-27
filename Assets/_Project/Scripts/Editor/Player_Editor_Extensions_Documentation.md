# ?? Player Editor Extensions - Character Stats & God Mode

## ? T?ng Quan
**Player Editor Extensions** là h? th?ng Custom Editor m? r?ng cho Unity Inspector, giúp hi?n th? và tu? ch?nh stats c?a player tr?c ti?p trong Editor cùng v?i tính n?ng **God Mode** ?? test game d? dàng.

## ?? Tính N?ng Chính

### ?? **Character Editor** 
Custom Editor cho `Character` component v?i các tính n?ng:

#### ?? **Player Stats Display**
- **?? Health System**:
  - Current Health (editable during runtime)
  - Max Health (editable during runtime)
  - Health percentage bar v?i visual feedback
  - Health regeneration rate control
  
- **?? Mana System**:
  - Current Mana (editable during runtime)
  - Max Mana (editable during runtime)  
  - Mana percentage bar v?i blue color
  - Mana regeneration rate control

- **?? Status Effects**:
  - Real-time display: Stunned, Knocked Back, Poisoned
  - Can Move status
  - Is Dead status

#### ??? **God Mode Feature**
- **Toggle Button**: B?t/t?t God Mode trong Editor
- **Invincibility**: Player không th? ch?t khi God Mode active
- **Health Lock**: Health t? ??ng restore v? max khi b? damage
- **Visual Feedback**: Warning message khi God Mode active
- **Auto Disable**: T? ??ng t?t khi exit Play Mode

#### ?? **Combat Settings**
Tu? ch?nh tr?c ti?p trong Editor:
- **Knockback Resistance**: ?i?u ch?nh kh? n?ng ch?ng knockback
- **Show Damage Numbers**: Toggle hi?n th? damage numbers
- **Screen Shake**: Toggle screen shake effects
- **Hit Stop**: Toggle hit stop effects
- **Damage Flash Settings**: 
  - Flash duration (th?i gian nh?p nháy)
  - Flash color (màu khi nh?n damage)

#### ?? **Debug Actions**
Các button ?? test nhanh:
- **Heal Buttons**: Heal 25, Heal 50, Full Heal
- **Damage Buttons**: Damage 10, 25, 50 (không ho?t ??ng khi God Mode)
- **Mana Buttons**: Restore Mana 25, Full Mana
- **Status Effects**: Stun 2s, Poison 5s

### ?? **PlayerController Editor**
Custom Editor cho `PlayerController` component v?i các tính n?ng:

#### ?? **Movement Settings**
- **Movement Status**: Real-time movement vector display
- **Is Busy/Free**: Hi?n th? tr?ng thái player
- **Can Move**: Status from Character component
- **Runtime Controls**:
  - Move Speed adjustment
  - Smooth Move Time slider (0.01 - 1.0)
  - Flip Smooth Time slider (0.01 - 0.5)

#### ? **Skill System Info**
- **Current Level**: Display player level
- **Unlocked Slots**: Show unlocked skill slots (X/8)
- **Available Skills**: Number of available skills
- **Equipped Skills**: List v?i hotkey và cooldown status
- **Real-time Cooldowns**: Live cooldown tracking

#### ?? **Leveling Controls**
- **Set Level**: Direct level input field
- **Quick Actions**: +1, +5, +10 level buttons
- **Preset Levels**: Level 10, 25, 50 buttons
- **Level Progression Info**:
  - Next slot unlock level
  - Levels per slot (5)
  - Max slots (8)

#### ?? **Debug Actions**
- **Movement Controls**: Force Stop, Set Busy, Set Free
- **Animation Tests**: Test Attack, Test Skill Animation
- **Component Validation**: Check all required components

## ??? **Installation & Setup**

### **File Structure**
```
Assets/_Project/Scripts/Editor/
??? CharacterEditor.cs          // Character custom editor
??? PlayerControllerEditor.cs   // PlayerController custom editor

Assets/_Project/Scripts/Core/
??? Character.cs               // Updated v?i public properties
```

### **Requirements**
- Unity Editor
- Character component v?i Resource health/mana
- PlayerController component
- ModularSkillManager component

### **Auto Setup**
Các Custom Editor s? t? ??ng áp d?ng khi:
- Character.cs ho?c PlayerController.cs ???c select trong Inspector
- Không c?n setup thêm gì

## ?? **Usage Examples**

### **Testing v?i God Mode**
```csharp
1. Select Player GameObject trong scene
2. Tìm Character component trong Inspector  
3. Trong "??? God Mode" section, toggle ON
4. Player gi? b?t t? và health luôn ? max
5. Test combat, boss fights, etc.
6. Toggle OFF khi mu?n test normal gameplay
```

### **Quick Leveling**
```csharp
1. Select Player GameObject
2. Tìm PlayerController component  
3. Trong "?? Leveling Controls" section
4. Click "+10 Levels" ?? unlock skill slots nhanh
5. Ho?c set direct level trong input field
```

### **Runtime Stats Monitoring**
```csharp
1. Enter Play Mode
2. Select Player trong Hierarchy
3. Xem real-time stats trong Inspector:
   - Health/Mana bars update live
   - Movement status changes
   - Skill cooldowns countdown
   - Status effects toggle on/off
```

## ?? **Visual Features**

### **Health/Mana Bars**
- **Progress Bars**: Visual representation c?a health/mana
- **Color Coding**: 
  - Health: Standard Unity progress bar
  - Mana: Blue custom bar
- **Text Labels**: Current/Max values v?i percentage

### **Status Indicators**
- **?/? Icons**: Clear visual cho boolean states
- **Color Coding**:
  - Green: Good status (Can Move, Free)
  - Red: Problem status (Busy, Dead)
  - Yellow: Warning (God Mode active)

### **Real-time Updates**
- **Auto Refresh**: Inspector updates during Play Mode
- **Live Values**: Stats change ngay khi có update
- **Immediate Feedback**: Button actions có instant response

## ?? **Technical Implementation**

### **Custom Editor Architecture**
```csharp
[CustomEditor(typeof(Character))]
public class CharacterEditor : Editor
{
    // Foldout states for UI organization
    private bool showStats = true;
    private bool showCombatSettings = true;
    private bool showDebugSettings = true;
    
    // God Mode implementation
    private bool godModeEnabled = false;
    private float originalHealth = 0f;
    
    public override void OnInspectorGUI()
    {
        // Custom Inspector implementation
    }
}
```

### **God Mode Implementation**
```csharp
private void ToggleGodMode(Character character, bool enabled)
{
    godModeEnabled = enabled;
    
    if (enabled)
    {
        // Save original health, set to max
        originalHealth = character.CurrentHealth;
        character.health.currentValue = character.health.maxValue;
    }
    else
    {
        // Restore original health
        if (originalHealth > 0)
            character.health.currentValue = originalHealth;
    }
}

private void Update()
{
    // Maintain god mode - keep health at max
    if (Application.isPlaying && godModeEnabled)
    {
        if (character.health.currentValue < character.health.maxValue)
            character.health.currentValue = character.health.maxValue;
    }
}
```

### **Real-time Updates**
```csharp
// Force repaint to keep UI updated
if (Application.isPlaying)
{
    EditorUtility.SetDirty(target);
    Repaint();
}
```

## ?? **Best Practices**

### **God Mode Usage**
1. **Testing Boss Fights**: Enable God Mode ?? test boss mechanics
2. **Level Design**: Test difficult areas without dying
3. **Skill Testing**: Focus on skill mechanics thay vì survival
4. **Remember to Disable**: T?t God Mode khi test balance

### **Level Controls**
1. **Gradual Testing**: Start with +1, +5 levels tr??c khi jump to high levels
2. **Skill Slot Testing**: Each 5 levels = 1 new slot
3. **Reset Testing**: Restart game ?? test from level 1

### **Performance**
1. **Play Mode Only**: Most features ch? available during Play Mode
2. **Auto Cleanup**: Editors t? cleanup khi exit Play Mode
3. **Minimal Impact**: Không affect build performance

## ?? **Troubleshooting**

### **Common Issues**

#### **"Custom Editor not showing"**
- **Solution**: Ensure files are in `Assets/.../Editor/` folder
- **Check**: Component names match exactly

#### **"God Mode not working"**
- **Solution**: Must be in Play Mode
- **Check**: Character component has health Resource

#### **"Stats not updating"**
- **Solution**: Select/deselect GameObject to refresh
- **Check**: Components are properly initialized

#### **"Missing Components warning"**
- **Solution**: Use "Validate Components" button
- **Add**: Required components (Character, PlayerController, etc.)

### **Debug Tips**
1. **Console Logs**: Check Console for debug messages
2. **Component Validation**: Use validation buttons
3. **Play Mode Required**: Many features need runtime

## ?? **Features Summary**

### ? **Implemented**
- Real-time health/mana display and editing
- God Mode with invincibility
- Movement settings control
- Skill system information display
- Level controls and quick actions
- Combat settings adjustment
- Debug actions and testing tools
- Status effects monitoring
- Component validation

### ?? **Benefits**
- **Developer Friendly**: Easy testing and debugging
- **Visual Feedback**: Clear status indicators
- **Time Saving**: Quick level up and god mode
- **Non-Destructive**: Doesn't affect build
- **Comprehensive**: Covers all major player systems

---

**?? V?i Player Editor Extensions, vi?c test và debug game tr? nên d? dàng và tr?c quan h?n bao gi? h?t!** ??????