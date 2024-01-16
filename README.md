# E4 Etilities

## 설치 방법

- [Git URL을 통한 설치 - Unity 매뉴얼](https://docs.unity3d.com/kr/2021.3/Manual/upm-ui-giturl.html)
- [Git 종속성 - Unity 매뉴얼](https://docs.unity3d.com/kr/2021.3/Manual/upm-git.html)

```
https://github.com/E4-Unity/com.eu4ng.utilities.git?path=/package
```

## 구성

### 1. DataManager

#### 사용 방법

- 저장할 데이터 클래스에 `ISavable` 인터페이스 상속
- 데이터 불러오기 : `LoadDataAsync` > `WaitForLoading` > `LoadData`
- 데이터 저장 : `SaveData` > `SaveAll`

#### API

T : class, ISavable, new()

- SaveData\<T\>()
- SaveDataImmediately\<T\>()
- SaveAll()
- SaveAllAsync()
- LoadData\<T\>()
- LoadDataAsync\<T\>()
- WaitForLoading()
- UnloadData\<T\>()
- DeleteData\<T\>()
- DeleteAll()

### 2. MonoSingleton

#### 사용 방법

- `MonoBehaviour` 대신 `MonoSingleton<T>` 상속
  - `UseDontDestroyOnLoad` 프로퍼티 설정
  - 초기화 : `InitializeComponent()` 오버라이드
- 싱글톤 인스턴스 가져오기 : `[MonoSingleton<T> 자식 클래스].Instance`
