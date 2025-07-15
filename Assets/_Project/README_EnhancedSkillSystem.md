# ?? Enhanced RPG Skill System 

## T?ng quan h? th?ng

H? th?ng k? n?ng RPG n�ng cao v?i 4 lo?i skill ch�nh:

### ??? **Melee (C?n chi?n)**
- T? ??ng sinh collider v�ng s�t th??ng xung quanh player
- Hi?n th? v�ng damage khi hover skill ho?c khi th?c thi
- S�t th??ng theo range t? v? tr� player

### ?? **Projectile (Ph�ng chi�u)** 
- V? v�ng tr�n b�n k�nh `maxRange` tr??c khi b?n
- Hi?n th? m?i t�n ch? ???ng t? player ??n cursor
- Projectile bay theo ???ng th?ng v?i t?c ?? `speed`

### ?? **AoE (Di?n r?ng)**
- V? v�ng s�t th??ng t?i v? tr� click chu?t
- S�t th??ng t?t c? enemy trong b�n k�nh `areaRadius`
- Preview v�ng ?nh h??ng tr??c khi cast

### ? **Support (H? tr?)**
- Kh�ng c?n v? v�ng s�t th??ng
- Th?c thi hi?u ?ng tr?c ti?p (heal, buff, v.v.)
- Hi?u ?ng visual ??c bi?t cho support skills

## ?? C�i ??t v� S? d?ng

### B??c 1: Th�m Components cho Player

```csharp
// Tr�n Player GameObject c?n c�:
- Character (existing)
- PlayerController (existing) 
- ModularSkillManager (existing)
- EnhancedSkillSystemManager (NEW)
- AttackableCharacter (NEW)
- GlobalCursorManager (NEW - c� th? ??t ri�ng)
```

### B??c 2: Thi?t l?p Enhanced Skill System Manager

1. **G?n EnhancedSkillSystemManager v�o Player**:
   ```csharp
   // Inspector settings:
   - Available Skills: K�o th? SkillModule assets
   - Damage Zone Prefab: (Optional) Custom prefab cho damage zones
   - Normal Cursor: Texture2D cho cursor b�nh th??ng  
   - Attack Cursor: Texture2D cho cursor t?n c�ng
   - Show Skill Preview: true (hi?n th? preview khi aim)
   - Show Range Indicator: true (hi?n th? v�ng tr�n range)
   - Enemy Layer Mask: Ch?n layer ch?a enemy (m?c ??nh layer 8)
   ```

### B??c 3: Thi?t l?p AttackableCharacter cho Enemies

```csharp
// Tr�n Enemy GameObjects:
1. Th�m AttackableCharacter component
2. Set "Can Be Attacked" = true
3. Set "Is Player" = false
// Component s? t? ??ng implement IAttackable interface
```

### B??c 4: Thi?t l?p Global Cursor Manager

```csharp
// T?o GameObject m?i v?i GlobalCursorManager:
1. T?o GameObject t�n "CursorManager"
2. Th�m GlobalCursorManager component  
3. G�n cursor textures
4. Set detection radius (m?c ??nh 1f)
5. Set enemy layer mask
```

### B??c 5: T?o Skills

#### C�ch 1: S? d?ng Sample Skill Creator
```csharp
1. T?o GameObject v?i SampleSkillCreator component
2. G�n default assets (icon, sounds, effects)
3. Check "Create Sample Skills" = true
4. Play game ? Skills s? ???c t?o t? ??ng
```

#### C�ch 2: T?o th? c�ng
```csharp
1. Right-click trong Project ? Create ? RPG ? Skill Module
2. Thi?t l?p properties:
   - Skill Name, Description, Icon
   - Skill Type (Melee/Projectile/Area/Support)
   - Combat Stats (damage, range, cooldown, etc.)
   - Visual & Audio (colors, sounds, effects)
```

## ?? C�ch s? d?ng Skills

### Input Controls

```csharp
// Trong PlayerController ho?c SkillManager:
- Tab: Toggle skill panel UI
- Mouse Click: Ch?n v� s? d?ng skill
- Right Click: H?y aiming
- V: Level up (+10 levels) cho testing
```

### API S? d?ng Skills

```csharp
// Th?c thi skill
EnhancedSkillSystemManager skillSystem = player.GetComponent<EnhancedSkillSystemManager>();
skillSystem.ExecuteSkill(skillModule, targetPosition);

// B?t ??u aiming
skillSystem.StartAiming(skillModule);

// K?t th�c aiming  
skillSystem.StopAiming();
```

## ?? Customization

### Override Damage Zones

```csharp
// Trong SkillModule:
1. T?o custom prefab cho damage zone
2. G�n v�o field "Damage Zone Prefab" 
3. H? th?ng s? d�ng custom prefab thay v� auto-generated
```

### Custom Cursor Textures

```csharp
// T?o cursor 32x32 pixels, format RGBA32:
1. Import texture, set Texture Type = Cursor
2. Set Read/Write = true
3. G�n v�o GlobalCursorManager ho?c EnhancedSkillSystemManager
```

### Custom Projectile Prefabs

```csharp
// Cho Projectile skills:
1. T?o prefab v?i visual components
2. G�n v�o SkillModule.projectilePrefab
3. EnhancedProjectileBehavior s? ???c add t? ??ng
```

## ?? Debugging

### Debug Options

```csharp
// Trong GlobalCursorManager:
- Show Debug Info: Hi?n th? logs chi ti?t
- Detection Radius: ?i?u ch?nh v�ng detect enemy

// Trong EnhancedSkillSystemManager:  
- Show Skill Preview: Hi?n th? preview khi aim
- Show Range Indicator: Hi?n th? v�ng tr�n range
```

### Common Issues

1. **Cursor kh�ng ??i khi hover enemy**:
   - Ki?m tra AttackableCharacter component tr�n enemy
   - Ki?m tra Enemy Layer Mask
   - Ki?m tra cursor textures ???c g�n

2. **Damage zone kh�ng hi?n th?**:
   - Ki?m tra SkillModule.showDamageArea = true
   - Ki?m tra damageAreaDisplayTime > 0
   - Ki?m tra Shader "Standard" c� s?n

3. **Skills kh�ng th?c thi**:
   - Ki?m tra mana ??
   - Ki?m tra level requirement
   - Ki?m tra CanExecute() conditions

## ?? File Structure

```
Assets/_Project/Scripts/
??? Core/
?   ??? IAttackable.cs (Interface cho attackable objects)
?   ??? AttackableCharacter.cs (Implementation cho Character) 
?   ??? EnhancedSkillSystemManager.cs (Main system manager)
?   ??? GlobalCursorManager.cs (Cursor management)
?   ??? SkillExecutors.cs (Updated v?i Support type)
??? Data/
?   ??? SkillModule.cs (Updated v?i damage zone override)
?   ??? SkillData.cs (Updated v?i Support enum)
??? Tools/
    ??? SampleSkillCreator.cs (Auto-create sample skills)
```

## ?? Performance Tips

1. **Object Pooling**: S? d?ng object pooling cho projectiles v� effects
2. **Batching**: Group damage zones c�ng material
3. **LOD**: Gi?m chi ti?t visual khi xa camera
4. **Culling**: ?n effects ngo�i camera view

## ?? Testing Checklist

- [ ] Player c� t?t c? required components
- [ ] Enemies c� AttackableCharacter component  
- [ ] Cursor textures ???c g�n ?�ng
- [ ] Sample skills ???c t?o th�nh c�ng
- [ ] Melee skills hi?n th? damage zone xung quanh player
- [ ] Projectile skills hi?n th? range circle v� direction arrow
- [ ] Area skills hi?n th? damage zone t?i v? tr� click
- [ ] Support skills kh�ng hi?n th? damage zone
- [ ] Cursor ??i khi hover enemy
- [ ] Animation "Attack" ???c trigger cho t?t c? skills

## ?? Support

N?u g?p v?n ??, ki?m tra:
1. Console logs v?i emoji indicators
2. Component references v� assignments
3. Layer masks v� collision settings  
4. Shader availability (Standard, Sprites/Default)

---

**Phi�n b?n**: Enhanced RPG Skill System v1.0  
**T??ng th�ch**: Unity 2021.3+ v?i .NET Framework 4.7.1