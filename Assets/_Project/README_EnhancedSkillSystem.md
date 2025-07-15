# ?? Enhanced RPG Skill System 

## T?ng quan h? th?ng

H? th?ng k? n?ng RPG nâng cao v?i 4 lo?i skill chính:

### ??? **Melee (C?n chi?n)**
- T? ??ng sinh collider vùng sát th??ng xung quanh player
- Hi?n th? vùng damage khi hover skill ho?c khi th?c thi
- Sát th??ng theo range t? v? trí player

### ?? **Projectile (Phóng chiêu)** 
- V? vòng tròn bán kính `maxRange` tr??c khi b?n
- Hi?n th? m?i tên ch? ???ng t? player ??n cursor
- Projectile bay theo ???ng th?ng v?i t?c ?? `speed`

### ?? **AoE (Di?n r?ng)**
- V? vùng sát th??ng t?i v? trí click chu?t
- Sát th??ng t?t c? enemy trong bán kính `areaRadius`
- Preview vùng ?nh h??ng tr??c khi cast

### ? **Support (H? tr?)**
- Không c?n v? vùng sát th??ng
- Th?c thi hi?u ?ng tr?c ti?p (heal, buff, v.v.)
- Hi?u ?ng visual ??c bi?t cho support skills

## ?? Cài ??t và S? d?ng

### B??c 1: Thêm Components cho Player

```csharp
// Trên Player GameObject c?n có:
- Character (existing)
- PlayerController (existing) 
- ModularSkillManager (existing)
- EnhancedSkillSystemManager (NEW)
- AttackableCharacter (NEW)
- GlobalCursorManager (NEW - có th? ??t riêng)
```

### B??c 2: Thi?t l?p Enhanced Skill System Manager

1. **G?n EnhancedSkillSystemManager vào Player**:
   ```csharp
   // Inspector settings:
   - Available Skills: Kéo th? SkillModule assets
   - Damage Zone Prefab: (Optional) Custom prefab cho damage zones
   - Normal Cursor: Texture2D cho cursor bình th??ng  
   - Attack Cursor: Texture2D cho cursor t?n công
   - Show Skill Preview: true (hi?n th? preview khi aim)
   - Show Range Indicator: true (hi?n th? vòng tròn range)
   - Enemy Layer Mask: Ch?n layer ch?a enemy (m?c ??nh layer 8)
   ```

### B??c 3: Thi?t l?p AttackableCharacter cho Enemies

```csharp
// Trên Enemy GameObjects:
1. Thêm AttackableCharacter component
2. Set "Can Be Attacked" = true
3. Set "Is Player" = false
// Component s? t? ??ng implement IAttackable interface
```

### B??c 4: Thi?t l?p Global Cursor Manager

```csharp
// T?o GameObject m?i v?i GlobalCursorManager:
1. T?o GameObject tên "CursorManager"
2. Thêm GlobalCursorManager component  
3. Gán cursor textures
4. Set detection radius (m?c ??nh 1f)
5. Set enemy layer mask
```

### B??c 5: T?o Skills

#### Cách 1: S? d?ng Sample Skill Creator
```csharp
1. T?o GameObject v?i SampleSkillCreator component
2. Gán default assets (icon, sounds, effects)
3. Check "Create Sample Skills" = true
4. Play game ? Skills s? ???c t?o t? ??ng
```

#### Cách 2: T?o th? công
```csharp
1. Right-click trong Project ? Create ? RPG ? Skill Module
2. Thi?t l?p properties:
   - Skill Name, Description, Icon
   - Skill Type (Melee/Projectile/Area/Support)
   - Combat Stats (damage, range, cooldown, etc.)
   - Visual & Audio (colors, sounds, effects)
```

## ?? Cách s? d?ng Skills

### Input Controls

```csharp
// Trong PlayerController ho?c SkillManager:
- Tab: Toggle skill panel UI
- Mouse Click: Ch?n và s? d?ng skill
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

// K?t thúc aiming  
skillSystem.StopAiming();
```

## ?? Customization

### Override Damage Zones

```csharp
// Trong SkillModule:
1. T?o custom prefab cho damage zone
2. Gán vào field "Damage Zone Prefab" 
3. H? th?ng s? dùng custom prefab thay vì auto-generated
```

### Custom Cursor Textures

```csharp
// T?o cursor 32x32 pixels, format RGBA32:
1. Import texture, set Texture Type = Cursor
2. Set Read/Write = true
3. Gán vào GlobalCursorManager ho?c EnhancedSkillSystemManager
```

### Custom Projectile Prefabs

```csharp
// Cho Projectile skills:
1. T?o prefab v?i visual components
2. Gán vào SkillModule.projectilePrefab
3. EnhancedProjectileBehavior s? ???c add t? ??ng
```

## ?? Debugging

### Debug Options

```csharp
// Trong GlobalCursorManager:
- Show Debug Info: Hi?n th? logs chi ti?t
- Detection Radius: ?i?u ch?nh vùng detect enemy

// Trong EnhancedSkillSystemManager:  
- Show Skill Preview: Hi?n th? preview khi aim
- Show Range Indicator: Hi?n th? vòng tròn range
```

### Common Issues

1. **Cursor không ??i khi hover enemy**:
   - Ki?m tra AttackableCharacter component trên enemy
   - Ki?m tra Enemy Layer Mask
   - Ki?m tra cursor textures ???c gán

2. **Damage zone không hi?n th?**:
   - Ki?m tra SkillModule.showDamageArea = true
   - Ki?m tra damageAreaDisplayTime > 0
   - Ki?m tra Shader "Standard" có s?n

3. **Skills không th?c thi**:
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

1. **Object Pooling**: S? d?ng object pooling cho projectiles và effects
2. **Batching**: Group damage zones cùng material
3. **LOD**: Gi?m chi ti?t visual khi xa camera
4. **Culling**: ?n effects ngoài camera view

## ?? Testing Checklist

- [ ] Player có t?t c? required components
- [ ] Enemies có AttackableCharacter component  
- [ ] Cursor textures ???c gán ?úng
- [ ] Sample skills ???c t?o thành công
- [ ] Melee skills hi?n th? damage zone xung quanh player
- [ ] Projectile skills hi?n th? range circle và direction arrow
- [ ] Area skills hi?n th? damage zone t?i v? trí click
- [ ] Support skills không hi?n th? damage zone
- [ ] Cursor ??i khi hover enemy
- [ ] Animation "Attack" ???c trigger cho t?t c? skills

## ?? Support

N?u g?p v?n ??, ki?m tra:
1. Console logs v?i emoji indicators
2. Component references và assignments
3. Layer masks và collision settings  
4. Shader availability (Standard, Sprites/Default)

---

**Phiên b?n**: Enhanced RPG Skill System v1.0  
**T??ng thích**: Unity 2021.3+ v?i .NET Framework 4.7.1