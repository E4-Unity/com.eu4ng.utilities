using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

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
        static readonly Dictionary<Type, ISavable> DataToSave = new Dictionary<Type, ISavable>();
        static readonly string SaveFolderPath = Path.Combine(Application.persistentDataPath, "DataManager");

        /* 초기화 */
        /// <summary>
        /// 애플리케이션 정지 혹은 종료 시 자동 저장
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        static void AutoSave()
        {
            Application.focusChanged += OnFocusChanged_Event;

            Application.quitting += OnQuitting_Event;
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

        /* API */

        /// <summary>
        /// DataToSave 에 저장할 데이터 추가
        /// </summary>
        /// <param name="data">저장할 데이터</param>
        public static void SaveData(ISavable data)
        {
            // 유효성 검사
            if (data is null) return;

            // 저장할 데이터 목록 확인
            var dataType = data.GetType();
            if (DataToSave.ContainsKey(dataType)) return;

            // 저장할 데이터 목록에 추가
            DataToSave.Add(dataType, data);
        }

        /// <summary>
        /// DataToSave 에 추가된 모든 데이터를 실제로 저장
        /// </summary>
        public static void SaveAll() => Task.WaitAll(SaveAllAsync());

        public static Task[] SaveAllAsync()
        {
            // 요청이 존재하는지 확인
            if (DataToSave.Count == 0) return new Task[]{ Task.CompletedTask };

            // 데이터 저장 작업 리스트
            List<Task> saveDataTasks = new List<Task>(DataToSave.Count);

            // 모든 데이터 저장 작업 시작
            foreach (var pair in DataToSave)
            {
                var saveDataTask = SaveDataAsync(pair.Value);
                saveDataTasks.Add(saveDataTask);
            }

            // 저장할 데이터 목록 정리
            DataToSave.Clear();

            // 모든 작업이 완료될 때까지 대기
            return saveDataTasks.ToArray();
        }

        /// <summary>
        /// 다음 우선 순위에 따라 데이터를 불러옵니다.
        /// 1. 데이터 변동 사항이 발생한 경우, 즉 DataToSave 에 등록된 데이터는 `메모리 상의 데이터`를 반환합니다.
        /// 2. 저장된 데이터 파일이 존재하지 않거나 올바르지 않은 형식으로 저장된 경우 `기본 생성자로 생성된 데이터`를 반환합니다.
        /// 3. `저장된 데이터`를 불러옵니다.
        /// </summary>
        /// <typeparam name="T">데이터 클래스</typeparam>
        public static T LoadData<T>() where T : ISavable, new()
        {
            var loadDataTask = LoadDataAsync<T>();
            loadDataTask.Wait();

            return loadDataTask.Result;
        }

        public static Task<T> LoadDataAsync<T>() where T : ISavable, new()
        {
            // 저장할 데이터 목록 확인
            if (DataToSave.TryGetValue(typeof(T), out var dataToSave))
            {
                // 데이터를 불러오는 대신 메모리 상의 데이터 반환
                return Task.FromResult((T)dataToSave);
            }

            // 저장된 데이터 파일이 존재하는지 확인
            var path = Path.Combine(SaveFolderPath, GetFileName(typeof(T)));
            if (!File.Exists(path)) return Task.FromResult(new T());

            // 데이터 파일 읽기
            var readAllTextTask = File.ReadAllTextAsync(path);
            readAllTextTask.Wait();

            // 데이터 불러오기
            var saveData = JsonUtility.FromJson<T>(readAllTextTask.Result);
            return saveData is null ? Task.FromResult(new T()) : Task.FromResult(saveData);
        }

        public static void DeleteData(Type dataType)
        {
            // 저장된 데이터 파일이 존재하는지 확인
            var path = Path.Combine(SaveFolderPath, GetFileName(dataType));
            if (!File.Exists(path)) return;

            // 데이터 파일 삭제
            File.Delete(path);
        }

#if UNITY_EDITOR
        [MenuItem("Tools/DataManager/Delete All")]
#endif
        public static void DeleteAll()
        {
            // 저장된 데이터 확인
            if (!Directory.Exists(SaveFolderPath)) return;

            var files = Directory.GetFiles(SaveFolderPath);

            // 데이터 삭제
            foreach (var file in files)
            {
                File.Delete(Path.Combine(SaveFolderPath, file));
            }
        }

        /* 메서드 */
        static string GetFileName(Type dataType) => dataType.Name + ".json";

        static Task SaveDataAsync(ISavable data)
        {
            // 유효성 검사
            if (data is null) return null;

            // JSON 데이터 생성
            var saveFilePath = Path.Combine(SaveFolderPath, GetFileName(data.GetType()));
            var jsonData = JsonUtility.ToJson(data, true);

            // 기존 저장 폴더가 없으면 새로 생성
            if (!Directory.Exists(SaveFolderPath))
            {
                Directory.CreateDirectory(SaveFolderPath);
            }

            // 데이터 저장
            var saveDataTask = File.WriteAllTextAsync(saveFilePath, jsonData);

            return saveDataTask;
        }
    }
}
