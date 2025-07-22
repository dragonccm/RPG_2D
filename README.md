# RPG Game Current

![Unity](https://img.shields.io/badge/engine-Unity-blue)
![.NET Framework](https://img.shields.io/badge/.NET-4.7.1-blue)

## Giới thiệu

**RPG Game Current** là một dự án game nhập vai 2D được phát triển bằng Unity, tập trung vào việc xây dựng một hệ thống game nhập vai hoàn chỉnh với các tính năng cốt lõi như hệ thống kỹ năng nâng cao, quản lý hiệu ứng chiến đấu, trí tuệ nhân tạo (AI) cho kẻ địch và giao diện người dùng (UI) hiện đại. Dự án này cung cấp một nền tảng vững chắc cho việc phát triển các tựa game RPG, tối ưu hóa trải nghiệm người chơi và dễ dàng mở rộng cho các nhà phát triển.

## Tổng quan dự án

### Vấn đề giải quyết

- Xây dựng một hệ thống kỹ năng, hiệu ứng và AI kẻ địch mạnh mẽ, linh hoạt và dễ mở rộng cho game nhập vai 2D.
- Đơn giản hóa quá trình quản lý kỹ năng, hiệu ứng hình ảnh và giao diện người dùng, giúp cả người chơi và nhà phát triển dễ dàng tương tác và tùy chỉnh.
- Cung cấp một cấu trúc dự án rõ ràng và dễ bảo trì cho các nhà phát triển game Unity.

### Tính năng chính

- **Hệ thống kỹ năng nâng cao**: Hỗ trợ đa dạng các loại kỹ năng (cận chiến, phép thuật, hỗ trợ, tức thời) với khả năng tùy biến cao, cho phép người chơi phát triển nhân vật theo nhiều hướng khác nhau.
- **Quản lý hiệu ứng chiến đấu**: Tích hợp các hiệu ứng va chạm, hiệu ứng theo dõi, tự động hủy và phân biệt chí mạng, mang lại trải nghiệm chiến đấu sống động và hấp dẫn.
- **AI kẻ địch thông minh**: Kẻ địch có khả năng di chuyển, tấn công, phòng thủ và phản ứng linh hoạt với hành động của người chơi, bao gồm các trạng thái như `IdleState`, `PatrolState`, `ChaseState`, `AttackState` và `RepositionState`.
- **Giao diện người dùng hiện đại**: Thiết kế UI trực quan và thân thiện, dễ dàng tương tác và cung cấp đầy đủ thông tin cần thiết cho người chơi.
- **Cấu trúc dự án mô-đun**: Mã nguồn được tổ chức rõ ràng theo từng module (AI, Controllers, Core, Managers, Systems, UI, v.v.), giúp dễ dàng phát triển, bảo trì và mở rộng.

### Công nghệ sử dụng

- **Engine**: Unity
- **Ngôn ngữ lập trình**: C#
- **Framework**: .NET Framework 4.7.1
- **Thư viện/Công cụ chính**:
  - **Addressable Assets**: Quản lý tài nguyên hiệu quả, tối ưu hóa tải game.
  - **NavMeshPlus**: Mở rộng khả năng điều hướng (Navigation Mesh) trong Unity cho các môi trường 2D.
  - **TextMesh Pro**: Hiển thị văn bản chất lượng cao trong UI.

## Cấu trúc thư mục

Dưới đây là cấu trúc thư mục chính của dự án, được tổ chức một cách logic để dễ dàng quản lý và phát triển:

```
/RPG_Game_Current
├── 📂 Assets/                               # Chứa tất cả tài nguyên của dự án Unity
│   ├── 📂 AddressableAssetsData/           # Dữ liệu cấu hình cho hệ thống Addressable Assets
│   ├── 📂 Editor/                          # Các script chỉ chạy trong môi trường Editor của Unity
│   │   └── 📄 BuildCommand.cs              # Script hỗ trợ quá trình build dự án
│   ├── 📂 Fantasy Wooden GUI  Free/       # Tài nguyên UI miễn phí theo phong cách gỗ
│   ├── 📂 NavMeshPlus-master/             # Thư viện NavMeshPlus cho điều hướng 2D
│   │   ├── 📄 LICENSE                      # Giấy phép của thư viện NavMeshPlus
│   │   ├── 📄 README.md                    # README của thư viện NavMeshPlus
│   │   └── 📄 package.json                 # Thông tin gói của NavMeshPlus
│   ├── 📂 Scenes/                          # Các cảnh (scene) trong game
│   │   └── 📄 SampleScene.unity            # Cảnh mẫu của dự án
│   ├── 📂 SkymonIconPackFree/             # Gói icon miễn phí
│   ├── 📂 Tests/                          # Các script kiểm thử (EditMode và PlayMode)
│   ├── 📂 TextMesh Pro/                   # Tài nguyên và cấu hình của TextMesh Pro
│   └── 📂 _Project/                       # Thư mục chính chứa mã nguồn và tài nguyên game
│       ├── 📂 Art/                        # Tài nguyên đồ họa (sprites, animations, v.v.)
│       ├── 📂 Audio/                      # Tài nguyên âm thanh và nhạc nền
│       ├── 📂 Data/                       # Các ScriptableObject và dữ liệu game
│       ├── 📂 Prefabs/                    # Các Prefab của game (nhân vật, vật phẩm, hiệu ứng)
│       ├── 📄 README_EnhancedSkillSystem.md # README chi tiết về hệ thống kỹ năng nâng cao
│       ├── 📄 README_SkillSystem.md       # README chi tiết về hệ thống kỹ năng
│       ├── 📂 Resources/                  # Tài nguyên được tải động (Resources folder)
│       ├── 📂 SETUP_SUMMARY.md            # Tóm tắt các bước cài đặt ban đầu
│       ├── 📂 Scenes/                     # Các cảnh game tùy chỉnh của dự án
│       ├── 📂 Scripts/                    # Toàn bộ mã nguồn C# của game
│       │   ├── 📂 AI/                    # Các script liên quan đến Trí tuệ nhân tạo của kẻ địch
│       │   │   ├── 📂 States/            # Các trạng thái AI (Idle, Attack, Chase, Reposition)
│       │   │   │   ├── 📄 AttackState.cs  # Trạng thái tấn công của kẻ địch
│       │   │   │   ├── 📄 ChaseState.cs   # Trạng thái truy đuổi của kẻ địch
│       │   │   │   ├── 📄 IdleState.cs    # Trạng thái chờ của kẻ địch
│       │   │   │   ├── 📄 PatrolState.cs  # Trạng thái tuần tra của kẻ địch
│       │   │   │   └── 📄 RepositionState.cs # Trạng thái di chuyển lại vị trí của kẻ địch
│       │   │   ├── 📄 EnemyAIController.cs # Điều khiển AI chung cho kẻ địch
│       │   │   ├── 📄 Enemy.cs            # Lớp cơ sở cho kẻ địch
│       │   │   ├── 📄 RangedEnemyAI.cs    # AI cụ thể cho kẻ địch tầm xa
│       │   │   └── 📄 StateMachine.cs     # Hệ thống máy trạng thái cho AI
│       │   ├── 📂 Controllers/            # Các script điều khiển (nhân vật, camera, v.v.)
│       │   │   ├── 📄 EnemyAttackController.cs # Điều khiển tấn công của kẻ địch
│       │   │   ├── 📄 EnemyMovementController.cs # Điều khiển di chuyển của kẻ địch
│       │   │   ├── 📄 PlayerController.cs # Điều khiển nhân vật người chơi
│       │   │   └── 📄 PlayerMovementController.cs # Điều khiển di chuyển của người chơi
│       │   ├── 📂 Core/                   # Các script cốt lõi của game
│       │   │   ├── 📄 GameState.cs        # Quản lý trạng thái game
│       │   │   ├── 📄 Health.cs           # Hệ thống máu và sát thương
│       │   │   └── 📄 State.cs            # Lớp cơ sở cho các trạng thái AI
│       │   ├── 📂 Data/                   # Các script định nghĩa dữ liệu (ScriptableObjects)
│       │   ├── 📂 Debug/                  # Các công cụ và script hỗ trợ debug
│       │   ├── 📂 Deprecated/             # Mã nguồn cũ hoặc không còn sử dụng
│       │   ├── 📂 Managers/               # Các script quản lý hệ thống (âm thanh, UI, game)
│       │   │   └── 📄 GameManager.cs      # Quản lý tổng thể game, tải cảnh, skill
│       │   ├── 📂 Systems/                # Các hệ thống game (skill, inventory, quest)
│       │   ├── 📂 Tools/                  # Các công cụ tiện ích
│       │   ├── 📂 Tutorial/               # Các script hướng dẫn
│       │   ├── 📂 UI/                     # Các script liên quan đến giao diện người dùng
│       │   └── 📂 Utils/                  # Các hàm tiện ích chung
│       └── 📂 Settings/                   # Các file cài đặt dự án tùy chỉnh
├── 📂 Packages/                           # Các gói Unity được quản lý bởi Package Manager
│   ├── 📄 manifest.json                   # Danh sách các gói phụ thuộc của dự án
│   └── 📄 packages-lock.json              # Khóa phiên bản các gói phụ thuộc
├── 📂 ProjectSettings/                    # Các file cài đặt của dự án Unity
├── 📄 .gitignore                          # Các file và thư mục bị bỏ qua bởi Git
├── 📄 .vsconfig                           # Cấu hình Visual Studio
├── 📄 ProjectAnalysisReport.md            # Báo cáo phân tích dự án
├── 📄 README.md                           # File README của dự án này
├── 📄 ScriptCleanupReport.md              # Báo cáo dọn dẹp script
└── 📄 build.bat                           # Script build dự án (nếu có)
```

## Luồng xử lý của hệ thống

Luồng xử lý chính của game RPG này tuân theo mô hình game Unity truyền thống, với sự tập trung vào quản lý trạng thái và tương tác giữa người chơi và kẻ địch:

1.  **Khởi tạo Game**: Khi game khởi động, <mcsymbol name="GameManager" filename="GameManager.cs" path="Assets/_Project/Scripts/Managers/GameManager.cs" startline="1" type="class"></mcsymbol> (một Singleton) được khởi tạo. Nó chịu trách nhiệm tải các cảnh cần thiết, khởi tạo hệ thống kỹ năng và các hệ thống quản lý khác.
2.  **Tải cảnh và Khởi tạo nhân vật**: <mcsymbol name="GameManager" filename="GameManager.cs" path="Assets/_Project/Scripts/Managers/GameManager.cs" startline="1" type="class"></mcsymbol> sẽ tải cảnh chơi chính. Trong cảnh này, nhân vật người chơi và các kẻ địch sẽ được khởi tạo từ Prefab.
3.  **Vòng lặp Game (Update Loop)**:
    *   **Người chơi**: <mcsymbol name="PlayerController" filename="PlayerController.cs" path="Assets/_Project/Scripts/Controllers/PlayerController.cs" startline="1" type="class"></mcsymbol> và <mcsymbol name="PlayerMovementController" filename="PlayerMovementController.cs" path="Assets/_Project/Scripts/Controllers/PlayerMovementController.cs" startline="1" type="class"></mcsymbol> xử lý đầu vào từ người chơi (di chuyển, tấn công, sử dụng kỹ năng). Các hành động này tương tác với hệ thống kỹ năng và hệ thống máu (<mcsymbol name="Health" filename="Health.cs" path="Assets/_Project/Scripts/Core/Health.cs" startline="1" type="class"></mcsymbol>).
    *   **Kẻ địch**: Mỗi kẻ địch (<mcsymbol name="Enemy" filename="Enemy.cs" path="Assets/_Project/Scripts/AI/Enemy.cs" startline="1" type="class"></mcsymbol>) có một <mcsymbol name="EnemyAIController" filename="EnemyAIController.cs" path="Assets/_Project/Scripts/AI/EnemyAIController.cs" startline="1" type="class"></mcsymbol> quản lý hành vi của nó thông qua một <mcsymbol name="StateMachine" filename="StateMachine.cs" path="Assets/_Project/Scripts/AI/StateMachine.cs" startline="1" type="class"></mcsymbol>. Các trạng thái AI (<mcsymbol name="IdleState" filename="IdleState.cs" path="Assets/_Project/Scripts/AI/States/IdleState.cs" startline="1" type="class"></mcsymbol>, <mcsymbol name="PatrolState" filename="PatrolState.cs" path="Assets/_Project/Scripts/AI/States/PatrolState.cs" startline="1" type="class"></mcsymbol>, <mcsymbol name="ChaseState" filename="ChaseState.cs" path="Assets/_Project/Scripts/AI/States/ChaseState.cs" startline="1" type="class"></mcsymbol>, <mcsymbol name="AttackState" filename="AttackState.cs" path="Assets/_Project/Scripts/AI/States/AttackState.cs" startline="1" type="class"></mcsymbol>, <mcsymbol name="RepositionState" filename="RepositionState.cs" path="Assets/_Project/Scripts/AI/RepositionState.cs" startline="1" type="class"></mcsymbol>) được chuyển đổi dựa trên khoảng cách đến người chơi, máu, và các điều kiện khác. <mcsymbol name="EnemyMovementController" filename="EnemyMovementController.cs" path="Assets/_Project/Scripts/Controllers/EnemyMovementController.cs" startline="1" type="class"></mcsymbol> và <mcsymbol name="EnemyAttackController" filename="EnemyAttackController.cs" path="Assets/_Project/Scripts/Controllers/EnemyAttackController.cs" startline="1" type="class"></mcsymbol> xử lý các hành động cụ thể của kẻ địch.
    *   **Hệ thống kỹ năng và hiệu ứng**: Khi kỹ năng được sử dụng hoặc hiệu ứng chiến đấu xảy ra, các script liên quan trong thư mục `Systems` và `UI` sẽ xử lý logic và cập nhật hiển thị.
4.  **Kết thúc Game**: Khi người chơi hoặc tất cả kẻ địch bị đánh bại, game sẽ chuyển sang trạng thái kết thúc (ví dụ: màn hình Game Over hoặc chiến thắng).

## Hướng dẫn cài đặt và sử dụng

Để chạy và phát triển dự án này, bạn cần cài đặt các công cụ và làm theo các bước dưới đây:

### Yêu cầu tiên quyết

-   **Unity Hub**: Đảm bảo bạn đã cài đặt Unity Hub.
-   **Unity Editor**: Phiên bản Unity Editor 2021.3.x (hoặc phiên bản tương thích với .NET Framework 4.7.1). Bạn có thể kiểm tra phiên bản Unity được sử dụng trong dự án bằng cách mở file `ProjectVersion.txt` trong thư mục `ProjectSettings`.
-   **Visual Studio** (hoặc IDE tương thích): Để chỉnh sửa mã C#.

### Cài đặt

1.  **Clone dự án**: Mở Terminal hoặc Git Bash và chạy lệnh sau:
    ```bash
    git clone https://github.com/your-username/RPG_Game_Current.git
    ```
    *(Lưu ý: Thay `https://github.com/your-username/RPG_Game_Current.git` bằng URL repository thực tế của bạn nếu có.)*

2.  **Di chuyển vào thư mục dự án**:
    ```bash
    cd RPG_Game_Current
    ```

3.  **Mở dự án bằng Unity Hub**: 
    *   Mở Unity Hub.
    *   Nhấn vào nút `Add Project` hoặc `Open`.
    *   Điều hướng đến thư mục `RPG_Game_Current` mà bạn vừa clone và chọn nó.
    *   Unity Hub sẽ tự động nhận diện và mở dự án với phiên bản Unity Editor phù hợp (nếu đã cài đặt).

4.  **Cài đặt các gói phụ thuộc (nếu cần)**:
    *   Unity sẽ tự động tải và cài đặt các gói phụ thuộc được liệt kê trong <mcfile name="manifest.json" path="Packages/manifest.json"></mcfile> khi bạn mở dự án lần đầu.
    *   Nếu có bất kỳ lỗi nào liên quan đến gói, bạn có thể kiểm tra `Window > Package Manager` trong Unity Editor để đảm bảo tất cả các gói đã được cài đặt đúng cách.

### Chạy dự án

1.  **Mở cảnh chính**: Trong Unity Editor, điều hướng đến thư mục `Assets/_Project/Scenes/` và mở cảnh `SampleScene.unity` (hoặc cảnh chính của game nếu có).
2.  **Chạy game**: Nhấn nút `Play` (biểu tượng tam giác) trên thanh công cụ của Unity Editor để bắt đầu chơi game.

### Chạy kiểm thử

Để chạy các bài kiểm thử tự động của dự án:

1.  Trong Unity Editor, mở cửa sổ Test Runner: `Window > General > Test Runner`.
2.  Trong cửa sổ Test Runner, bạn có thể chọn chạy các bài kiểm thử ở chế độ `EditMode` hoặc `PlayMode`.
3.  Nhấn nút `Run All` để chạy tất cả các bài kiểm thử hoặc chọn từng bài để chạy riêng lẻ.

## Giấy phép

Dự án này được phát hành dưới giấy phép MIT. Xem file <mcfile name="LICENSE" path="NavMeshPlus-master/LICENSE"></mcfile> để biết thêm chi tiết.

## Liên hệ

Mọi thắc mắc hoặc góp ý, vui lòng liên hệ qua:

-   **Email**: [your-email@example.com](mailto:your-email@example.com)
-   **GitHub**: [your-github-profile](https://github.com/your-github-profile)

*(Lưu ý: Thay thế thông tin liên hệ bằng của bạn.)*
