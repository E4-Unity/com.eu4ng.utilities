using UnityEngine;

namespace E4.Utilities
{
    /// <summary>
    /// MonoBehaviour 를 상속받은 제네릭 싱글톤 클래스이다.
    /// 자동 인스턴싱은 지원하지 않는다.
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        /* 정적 필드 */
        static T m_Instance;

        /* 정적 프로퍼티 */
        public static T Instance
        {
            get
            {
                // 이미 할당된 상태라면 그대로 반환
                if (m_Instance is not null) return m_Instance;

                // 씬에 배치되어 있는 컴포넌트 찾기
                Instance = FindObjectOfType<T>();
                return m_Instance;
            }
            private set
            {
                m_Instance = value;
                m_Instance?.TryInit();
            }
        }

        /* 필드 */
        bool isInitialized;

        /* 프로퍼티 */
        bool IsInstance => ReferenceEquals(m_Instance, this);

        protected virtual bool UseDontDestroyOnLoad => false;

        /* MonoBehaviour */
        protected virtual void Awake()
        {
            if (m_Instance is null)
            {
                Instance = (T)this;
            }
            else if (!IsInstance)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (IsInstance)
            {
                Instance = null;
            }
        }

        /* 메서드 */
        void TryInit()
        {
            // 초기화는 한 번만 진행
            if (isInitialized) return;
            isInitialized = true;

            // 루트 게임 오브젝트 DontDestroyOnLoad 설정
            if (UseDontDestroyOnLoad)
            {
                var root = transform.root.gameObject;
                if(root.scene.name != "DontDestroyOnLoad") DontDestroyOnLoad(root);
            }

            // 커스텀 초기화
            InitializeComponent();
        }

        protected virtual void InitializeComponent(){}
    }
}
