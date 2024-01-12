using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace E4.Utilities
{
    /// <summary>
    /// 저장할 데이터 클래스에 구현할 인터페이스
    /// </summary>
    public interface ISavable
    {

    }

    // TODO 데이터 저장 경로 변경 기능 추가
    // TODO 싱글톤 버전 제작
    // TODO UnityEditor 분리
    // TODO JSON 보안
    /// <summary>
    /// - 데이터 저장, 불러오기, 삭제 기능을 `정적 메서드` 로 제공
    /// - 저장할 데이터 클래스에 `ISavable` 인터페이스 구현 필요
    /// - 데이터 저장 경로는 `PersistentDataPath/DataManager/[데이터 클래스 이름].json` 입니다.
    /// - `SaveData` 메서드 호출 시 데이터가 바로 저장되는 것이 아니라 `DataToSave` 에 등록되고, `SaveAll` 메서드 호출 시 실제 데이터 저장이 이루어집니다.
    /// - 기본적으로 애플리케이션이 정지되거나 종료되는 경우 `SaveAll` 메서드가 자동적으로 호출되며, 원하는 타이밍에 직접 호출하는 것 역시 가능합니다.
    /// - 내부적으로 데이터 저장은 모두 비동기 방식으로 이루어집니다.
    /// </summary>
    public static class DataManager
    {
        /* 필드 */
        static readonly string m_SaveFolderPath = Path.Combine(Application.persistentDataPath, "DataManager"); // 데이터 저장 폴더 경로

        static readonly Dictionary<Type, object> m_LoadedData = new Dictionary<Type, object>(); // 로딩된 데이터 캐시
        static readonly HashSet<Type> m_DataToSave = new HashSet<Type>(); // 저장이 필요한 데이터

        static readonly Dictionary<Type, Task> m_LoadDataTasks = new Dictionary<Type, Task>(); // LoadDataAsync 호출 시 Task 가 추가됨

        /* 초기화 */
        [RuntimeInitializeOnLoadMethod]
        static void RuntimeInit()
        {
            // 자동 저장
            Application.focusChanged += OnFocusChanged_Event;
            Application.quitting += OnQuitting_Event;

            // Start 이벤트 때 모든 데이터 로딩 작업 완료 보장
            SceneManager.sceneLoaded += OnSceneLoaded_Event;
        }

        /* 이벤트 함수 */
        static void OnFocusChanged_Event(bool focus)
        {
#if UNITY_EDITOR
            // 에디터에서는 자동으로 초기화되지 않는다
            if (!Application.isPlaying)
            {
                Application.focusChanged -= OnFocusChanged_Event;
            }
#endif
            // 어플리케이션 정지 시 모든 데이터 저장
            if(!focus) SaveAll();
        }

        static void OnQuitting_Event()
        {
#if UNITY_EDITOR
            // 에디터에서는 자동으로 초기화되지 않는다
            if (!Application.isPlaying)
            {
                Application.quitting -= OnQuitting_Event;
            }
#endif
            // 어플리케이션 종료 시 모든 데이터 저장
            SaveAll();
        }

        static void OnSceneLoaded_Event(Scene scene, LoadSceneMode loadSceneMode)
        {
            WaitForLoading();
        }

        /* API */
        /// <summary>
        /// 호출된 시점에 진행중인 모든 비동기 데이터 로딩 작업이 완료될 때까지 대기
        /// </summary>
        public static async void WaitForLoading()
        {
            // 진행중인 모든 비동기 데이터 로딩 작업이 완료될 때까지 대기
            if (m_LoadDataTasks.Count == 0) return;
            await Task.WhenAll(m_LoadDataTasks.Values);

            // 초기화
            m_LoadDataTasks.Clear();
        }

        /// <summary>
        /// 저장이 필요한 데이터 표기
        /// </summary>
        public static void SaveData<T>() where T : class, ISavable, new()
        {
            // 저장할 데이터 목록 확인
            var dataType = typeof(T);
            if (m_DataToSave.Contains(dataType)) return;

            // 저장할 데이터 목록에 추가
            m_DataToSave.Add(dataType);
        }

        /// <summary>
        /// 데이터 즉시 저장 (비동기 방식)
        /// </summary>
        public static void SaveDataImmediately<T>() where T : class, ISavable, new()
        {
            var dataType = typeof(T);

            // 저장할 데이터 목록에 등록되어 있는 경우 목록에서 제거
            if (m_DataToSave.Contains(dataType)) m_DataToSave.Remove(dataType);

            // 비동기 데이터 저장
            SaveDataAsync(m_LoadedData[dataType]);
        }

        /// <summary>
        /// 저장이 필요한 모든 데이터를 동기 방식으로 저장
        /// </summary>
        public static void SaveAll() => Task.WaitAll(SaveAllAsync());

        /// <summary>
        /// 저장이 필요한 모든 데이터를 비동기 방식으로 저장
        /// </summary>
        public static Task[] SaveAllAsync()
        {
            // 요청이 존재하는지 확인
            if (m_DataToSave.Count == 0) return new Task[]{ Task.CompletedTask };

            // 데이터 저장 작업 리스트
            List<Task> saveDataTasks = new List<Task>(m_DataToSave.Count);

            // 모든 데이터 저장 작업 시작
            foreach (var dataType in m_DataToSave)
            {
                var saveDataTask = SaveDataAsync(m_LoadedData[dataType]);
                saveDataTasks.Add(saveDataTask);
            }

            // 저장할 데이터 목록 정리
            m_DataToSave.Clear();

            // 모든 작업이 완료될 때까지 대기
            return saveDataTasks.ToArray();
        }

        /// <summary>
        /// 동기 방식으로 데이터 로딩
        /// 1. 데이터 로딩 작업이 진행중인 경우, 데이터 로딩 작업이 완료될 때까지 대기한 후 로드된 데이터를 반환합니다.
        /// 2. LoadDataAsync 메서드를 호출한 후 완료될 때까지 대기합니다.
        /// </summary>
        public static T LoadData<T>() where T : class, ISavable, new()
        {
            var dataType = typeof(T);

            // 기존 데이터 로딩 작업이 존재하면 완료될 때까지 대기 및 완료 후 작업 목록에서 제거
            if (m_LoadDataTasks.TryGetValue(dataType, out var task))
            {
                task.Wait();
                m_LoadDataTasks.Remove(dataType);

                return (T)m_LoadedData[dataType];
            }

            // 데이터 로딩 (캐시 확인 포함)
            var loadDataTask = LoadDataAsync<T>();
            loadDataTask.Wait();

            // 로드된 데이터 반환
            return loadDataTask.Result;
        }

        /// <summary>
        /// 비동기 방식으로 데이터 로딩
        /// 1. 이미 로딩이 완료된 데이터의 경우 캐시 데이터를 반환합니다.
        /// 2. 저장된 데이터 파일이 존재하지 않거나 올바르지 않은 데이터인 경우 기본 생성자로 생성된 데이터를 반환합니다.
        /// 3. 저장된 데이터 파일로부터 데이터를 불러옵니다.
        /// </summary>
        public static Task<T> LoadDataAsync<T>() where T : class, ISavable, new()
        {
            var dataType = typeof(T);

            // 중복 호출 방지
            if (m_LoadDataTasks.TryGetValue(dataType, out var uncompletedTask)) return (Task<T>)uncompletedTask;

            // 1. 캐시 확인
            if (m_LoadedData.TryGetValue(dataType, out var loadedData))
            {
                // 캐시 데이터가 포함된 Completed Task 반환
                return Task.FromResult((T)loadedData);
            }

            // 2. 저장된 데이터 파일이 존재하지 않는 경우 기본 데이터 반환
            var path = Path.Combine(m_SaveFolderPath, GetFileName(dataType));
            if (!File.Exists(path))
            {
                // 새로운 데이터 생성
                var newData = new T();

                // 캐시 저장
                m_LoadedData.Add(dataType, newData);

                // 새로운 데이터가 포함된 Completed Task 반환
                return Task.FromResult(newData);
            }

            // 3. 데이터 파일에서 데이터를 불러오거나 실패할 경우 기본 데이터 반환
            var loadDataTask = File.ReadAllTextAsync(path).ContinueWith(readTextTask =>
            {
                // 데이터 불러오기
                var saveData = JsonUtility.FromJson<T>(readTextTask.Result);
                var result = saveData ?? new T();

                // 캐시 저장
                m_LoadedData.Add(dataType, result);

                // 불러온 데이터 반환
                return result;
            });

            // 데이터 로딩 작업 목록 추가
            m_LoadDataTasks.Add(dataType, loadDataTask);

            // 데이터 로딩 작업 반환
            return loadDataTask;
        }

        /// <summary>
        /// 데이터를 언로드합니다. 저장이 필요한 경우 자동으로 저장합니다.
        /// </summary>
        public static void UnloadData<T>() where T : class, ISavable, new()
        {
            var dataType = typeof(T);

            // 데이터 저장 요청 목록에 존재하는 경우 목록에서 제거 및 저장
            if (m_DataToSave.Contains(dataType))
            {
                m_DataToSave.Remove(dataType);
                SaveDataAsync(m_LoadedData[dataType]);
            }

            // 데이터 언로드
            m_LoadedData.Remove(dataType);
        }

        /// <summary>
        /// 특정 데이터 파일 삭제
        /// </summary>
        public static void DeleteData<T>() where T : class, ISavable, new()
        {
            var dataType = typeof(T);

            // 저장된 데이터 파일이 존재하는지 확인
            var path = Path.Combine(m_SaveFolderPath, GetFileName(dataType));
            if (!File.Exists(path)) return;

            // 데이터 파일 삭제
            File.Delete(path);
        }

        /// <summary>
        /// 데이터 저장 폴더 비우기
        /// </summary>
#if UNITY_EDITOR
        [MenuItem("Tools/DataManager/Delete All")]
#endif
        public static void DeleteAll()
        {
            // 저장된 데이터 확인
            if (!Directory.Exists(m_SaveFolderPath)) return;

            var files = Directory.GetFiles(m_SaveFolderPath);

            // 데이터 삭제
            foreach (var file in files)
            {
                File.Delete(Path.Combine(m_SaveFolderPath, file));
            }
        }

        /* 메서드 */
        static string GetFileName(Type dataType) => dataType.Name + ".json";

        static Task SaveDataAsync(object data)
        {
            // 유효성 검사
            if (data is null) return null;

            // JSON 데이터 생성
            var saveFilePath = Path.Combine(m_SaveFolderPath, GetFileName(data.GetType()));
            var jsonData = JsonUtility.ToJson(data, true);

            // 기존 저장 폴더가 없으면 새로 생성
            if (!Directory.Exists(m_SaveFolderPath))
            {
                Directory.CreateDirectory(m_SaveFolderPath);
            }

            // 데이터 저장
            var saveDataTask = File.WriteAllTextAsync(saveFilePath, jsonData);

            return saveDataTask;
        }
    }
}
