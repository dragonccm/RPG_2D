# 🚀 Hướng dẫn Sử dụng Auto-Detection cho DialogueUI

## 🎯 Tổng quan

Tính năng **Auto-Detection** cho phép DialogueUI tự động tìm và gán tất cả UI components chỉ bằng cách bạn kéo Canvas tổng vào 1 field duy nhất!

## 📋 Cách sử dụng

### Bước 1: Thêm DialogueUI Script
```
1. Click vào Canvas chính của bạn
2. Inspector → Add Component → DialogueUI
```

### Bước 2: Bật Auto-Detection
```
Trong Inspector của DialogueUI:
- Tìm section "Auto Detection (Khuyến nghị)"
- Check vào ô "Auto Detect Components"
- Kéo Canvas tổng vào field "Dialogue Canvas"
```

### Bước 3: Chạy Auto-Detection
```
Click button "Auto Detect Components" trong Inspector
HOẶC
Script sẽ tự động detect khi game chạy (Awake)
```

### Bước 4: Validate Setup
```
Click button "Validate Setup" để kiểm tra
Xem Console để biết kết quả
```

---

## 🎨 Naming Convention (Quan trọng!)

Để auto-detection hoạt động tốt nhất, hãy đặt tên các GameObject theo quy tắc sau:

### **Dialogue Panel:**
```
✅ Tốt:
- DialoguePanel
- Dialogue
- Panel
- MainPanel
- DialogPanel
- ChatPanel
- ConversationPanel

❌ Không tốt:
- GameObject
- UI
- Stuff
```

### **Text Elements:**
```
✅ Dialogue Text:
- DialogueText
- Dialogue
- Text

✅ NPC Name Text:
- NPCNameText
- NPCName
- NameText
- SpeakerName

✅ Continue Text:
- ContinueText
- Continue
- NextText
```

### **Image Elements:**
```
✅ NPC Portrait:
- NPCPortrait
- Portrait
- Avatar
- NPCImage

✅ Background:
- BackgroundImage
- Background
- BG
```

### **Control Elements:**
```
✅ Continue Button:
- ContinueButton
- Continue
- NextButton

✅ Choice Panel:
- ChoicePanel
- Choices
- Choice

✅ Choice Container:
- ChoiceContainer
- Container
- Choices
```

---

## 🔍 Logic Tìm Kiếm

### **Thứ tự ưu tiên:**
1. **Tên chính xác** - Tìm exact match
2. **Pattern matching** - Tìm theo pattern (dialogue, panel, etc.)
3. **Component type** - Tìm theo loại component (TextMeshProUGUI, Button, etc.)
4. **Fallback** - Tạo default nếu không tìm thấy

### **Ví dụ tìm kiếm Dialogue Text:**
```
1. Tìm "DialogueText" (exact)
2. Tìm "Dialogue" (exact)
3. Tìm "Text" (exact)
4. Tìm bất kỳ TextMeshProUGUI nào trong children
5. Log warning nếu không tìm thấy
```

---

## 🎮 Demo Sử dụng

### **Cách 1: Setup trong Editor**

```csharp
// 1. Tạo Canvas với UI elements
// 2. Thêm DialogueUI script
// 3. Kéo Canvas vào field "Dialogue Canvas"
// 4. Click "Auto Detect Components"
// 5. Click "Validate Setup"
```

### **Cách 2: Setup bằng Code**

```csharp
using DialogueSystem;

public class QuickDialogueSetup : MonoBehaviour
{
    [SerializeField] private GameObject dialogueCanvas;

    void Start()
    {
        // Thêm DialogueUI script
        DialogueUI dialogueUI = dialogueCanvas.AddComponent<DialogueUI>();

        // Bật auto-detection
        dialogueUI.SetAutoDetectCanvas(dialogueCanvas);
        dialogueUI.AutoDetectComponents();
        dialogueUI.ValidateSetup();
    }
}
```

---

## 📊 Kết quả Auto-Detection

### **Console Output:**
```
🔍 Auto-detecting Dialogue UI components...
📋 Found dialogue panel: DialoguePanel
📝 Found text component: DialogueText
📝 Found text component: NPCNameText
🖼️ Found image component: NPCPortrait
🔘 Found button component: ContinueButton
📋 Found panel component: ChoicePanel
📂 Found container component: ChoiceContainer
🔘 Found choice button prefab: ChoiceButton
✅ Auto-detection completed!
```

### **Validation Output:**
```
🔍 Validating Dialogue UI setup...
✅ Dialogue Panel: Found
✅ Dialogue Text: Found
✅ NPC Name Text: Found
✅ NPC Portrait: Found
✅ Continue Button: Found
✅ Choice Panel: Found
✅ Choice Container: Found
✅ Choice Button Prefab: Found
🎉 Setup validation PASSED! Found 8 components.
```

---

## ⚙️ Advanced Settings

### **Show Advanced Setup:**
```
Check vào "Show Advanced Setup" để:
- Thấy tất cả fields chi tiết
- Có thể manual override auto-detected components
- Tùy chỉnh settings nâng cao
```

### **Custom Search Patterns:**
```csharp
// Bạn có thể extend class để thêm patterns tùy chỉnh
public class CustomDialogueUI : DialogueUI
{
    protected override void AutoDetectComponents()
    {
        base.AutoDetectComponents();

        // Thêm custom detection logic
        // dialogueText = FindCustomTextComponent();
    }
}
```

---

## 🔧 Troubleshooting

### **"Không tìm thấy component nào"**
```
Nguyên nhân:
- Tên GameObject không theo convention
- Component không phải TextMeshProUGUI/Button/Image

Giải pháp:
- Đổi tên GameObject theo hướng dẫn
- Kiểm tra component type
- Sử dụng manual assignment
```

### **"Tìm thấy sai component"**
```
Nguyên nhân:
- Nhiều components cùng loại
- Thứ tự tìm kiếm không mong muốn

Giải pháp:
- Đổi tên để ưu tiên component đúng
- Sử dụng manual assignment cho component đó
- Check "Show Advanced Setup" để override
```

### **"Script không tự động detect"**
```
Nguyên nhân:
- Chưa bật "Auto Detect Components"
- Chưa gán Dialogue Canvas
- Script bị disable

Giải pháp:
- Check "Auto Detect Components"
- Gán Dialogue Canvas field
- Enable script
- Restart scene
```

---

## 🎯 Best Practices

### **1. Naming Convention:**
```
Luôn đặt tên theo convention để auto-detection hoạt động tốt nhất
```

### **2. Hierarchy Structure:**
```
DialogueCanvas
├── DialoguePanel
│   ├── BackgroundImage (optional)
│   ├── NPCPortrait
│   ├── NPCNameText
│   ├── DialogueText
│   ├── ContinueButton
│   │   └── ContinueText
│   └── ChoicePanel
│       └── ChoiceContainer
```

### **3. Component Types:**
```
- Text: TextMeshProUGUI (không phải Unity Text)
- Buttons: Unity Button component
- Images: Unity Image component
- Panels: GameObject với Image hoặc child components
```

### **4. Testing:**
```
Luôn chạy Validate Setup sau khi auto-detect
Test dialogue với sample data
Check Console logs
```

---

## 🚀 Quick Start Guide

### **5 bước để setup DialogueUI:**

1. **Tạo UI elements** với naming convention
2. **Thêm DialogueUI script** vào Canvas
3. **Kéo Canvas vào field** "Dialogue Canvas"
4. **Click "Auto Detect Components"**
5. **Click "Validate Setup"** và check Console

### **Test ngay:**
```csharp
// Tạo test dialogue
DialogueData dialogue = ScriptableObject.CreateInstance<DialogueData>();
DialogueLine line = new DialogueLine("Xin chào! Auto-detection hoạt động!", "NPC");
dialogue.DialogueLines.Add(line);
DialogueManager.Instance.StartDialogue(dialogue);
```

---

## 🎉 Kết luận

Với tính năng **Auto-Detection**, việc setup DialogueUI giờ chỉ cần:

1. **1 field duy nhất** - Dialogue Canvas
2. **1 click** - Auto Detect Components  
3. **1 click** - Validate Setup

**Không còn phải kéo từng field một cách thủ công!** 🎊

**💡 Tip:** Nếu UI của bạn đã có sẵn, chỉ cần đổi tên các GameObject theo convention là auto-detection sẽ hoạt động hoàn hảo!</content>
<parameter name="filePath">c:\Users\nguye\RPG_Game_Current\RPG_Game_Current\Assets\_Project\Scripts\DialogueSystem\DialogueUI_AutoDetection_Guide.md
