using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace E4.Utilities
{
    /// <summary>
    /// DataManager 를 사용하는 클래스에 상속
    /// </summary>
    /// <typeparam name="T"> 저장할 데이터 클래스 </typeparam>
    public interface ISavable<out T> where T : class
    {
        static readonly string FileName = typeof(T) + ".json";
        T Data { get; }
    }

    /// <summary>
    /// 데이터 저장 및 불러오기를 담당하는 클래스
    /// </summary>
    public abstract class DataManager
    {
        /* 필드 */
        static readonly string DefaultDataPath = Application.persistentDataPath;
        static readonly Queue<Action> Requests = new Queue<Action>();
        static readonly HashSet<object> Targets = new HashSet<object>();

        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
            // 어플리케이션 정지 혹은 종료 시 자동 저장
            Application.focusChanged += focus =>
            {
                if(!focus) HandleRequests();
            };

            Application.quitting += HandleRequests;
        }

        /* 데이터 저장 */
        public static bool RequestSaveData<T>(ISavable<T> target) where T : class => RequestSaveData(target, DefaultDataPath);
        public static bool RequestSaveData<T>(ISavable<T> target, string filePath) where T : class
        {
            // 더티 플래그 확인 및 설정
            if (target is null) return false;

            if (Targets.Contains(target))
            {
#if UNITY_EDITOR
                Debug.Log("<color=yellow>이미 데이터 저장이 요청된 상태입니다 : " + target + "</color>");
#endif
                return false;
            }

            Targets.Add(target);

#if UNITY_EDITOR
            Debug.Log("<color=yellow>데이터 저장 요청이 승낙되었습니다 : " + target + "</color>");
#endif

            // 요청 목록에 작업 추가
            Requests.Enqueue(() =>
            {
                SaveData(target, filePath);
            });

            return true;
        }
        public static void HandleRequests()
        {
            // 요청이 존재하는 경우에만 실행
            if (Requests.Count == 0) return;

#if UNITY_EDITOR
            Debug.Log("<color=red>모든 데이터 저장 요청을 처리합니다</color>");
#endif

            // 요청 실행
            while (Requests.Count != 0)
            {
                var request = Requests.Dequeue();
                request.Invoke();
            }
        }

        static void SaveData<T>(ISavable<T> target) where T : class => SaveData(target, DefaultDataPath);

        static async void SaveData<T>(ISavable<T> target, string filePath) where T : class
        {
            // 유효성 검사
            if (target is null) return;

            // JSON 데이터 저장
            var path = Path.Combine(filePath, ISavable<T>.FileName);

            var json = JsonUtility.ToJson(target.Data, true);
            await File.WriteAllTextAsync(path, json);

#if UNITY_EDITOR
            Debug.Log("<color=green>데이터가 저장되었습니다 : " + path + "</color>");
#endif
        }

        /* 데이터 불러오기 */
        public static T LoadData<T>(ISavable<T> target) where T : class => LoadData<T>(target, DefaultDataPath);

        // TODO 비동기
        public static T LoadData<T>(ISavable<T> target, string filePath) where T : class
        {
            // 유효성 검사
            if (target is null) return null;

            // 저장된 데이터 확인
            var path = Path.Combine(filePath, ISavable<T>.FileName);
            if (!File.Exists(path)) return null;

#if UNITY_EDITOR
            Debug.Log("<color=green>데이터를 불러왔습니다 : " + path + "</color>");
#endif

            // 데이터 불러오기
            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }

        /* 데이터 삭제 */
        public static void DeleteData(string fileName) => DeleteData(fileName, DefaultDataPath);

        public static void DeleteData(string fileName, string filePath)
        {
            // 저장된 데이터 확인
            var path = Path.Combine(filePath, fileName);
            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.Log("<color=green>저장된 데이터가 없습니다 : " + path + "</color>");
#endif
                return;
            }

            // 데이터 삭제
            File.Delete(path);

#if UNITY_EDITOR
            Debug.Log("<color=green>데이터를 삭제했습니다 : " + path + "</color>");
#endif
        }

        // TODO DataManagerBase 에서 처리한 데이터만 삭제
        public static void DeleteAllData()
        {
            // 저장된 데이터 확인
            if (!Directory.Exists(DefaultDataPath)) return;

            var files = Directory.GetFiles(DefaultDataPath);

            // 데이터 삭제
            foreach (var file in files)
            {
                File.Delete(Path.Combine(DefaultDataPath, file));
            }

#if UNITY_EDITOR
            Debug.Log("<color=green>모든 데이터를 삭제했습니다 : " + DefaultDataPath + "</color>");
#endif
        }
    }
}
