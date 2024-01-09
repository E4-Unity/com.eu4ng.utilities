using UnityEngine;

namespace E4.Utilities.Samples
{
    public class GoodsSaveData : ISavable
    {
        public int Gold;
    }

    public class GoodsManager : MonoBehaviour
    {
        /* 필드 */
        GoodsSaveData m_Data;

        /* 프로퍼티 */
        GoodsSaveData Data => m_Data;

        int Gold
        {
            get => Data.Gold;
            set
            {
                Data.Gold = value;
                DataManager.SaveData<GoodsSaveData>();
            }
        }

        /* MonoBehaviour */
        void Awake()
        {
            // 데이터 로딩
            DataManager.LoadDataAsync<GoodsSaveData>();
        }

        void Start()
        {
            m_Data = DataManager.LoadData<GoodsSaveData>();

            print(Gold);

            Gold = 61;
        }

        void OnDestroy()
        {
            DataManager.UnloadData<GoodsSaveData>();
        }
    }
}
