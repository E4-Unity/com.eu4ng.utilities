using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
#endif

namespace Eu4ng.Utilities
{
    public static class LogUtilities
    {
#if LOG_UTILITIES
        public static void Log(object message)
        {
            Debug.Log(message);
        }

        public static void Log(object message, Object context)
        {
            Debug.Log(message, context);
        }

        public static void LogFormat(string message, params object[] args)
        {
            Debug.LogFormat(message, args);
        }

        public static void LogFormat(Object context, string message, params object[] args)
        {
            Debug.LogFormat(context, message, args);
        }

        public static void LogWarning(object message)
        {
            Debug.LogWarning(message);
        }

        public static void LogWarning(object message, Object context)
        {
            Debug.LogWarning(message, context);
        }

        public static void LogWarningFormat(string message, params object[] args)
        {
            Debug.LogWarningFormat(message, args);
        }

        public static void LogWarningFormat(Object context, string message, params object[] args)
        {
            Debug.LogWarningFormat(context, message, args);
        }

        public static void LogError(object message)
        {
            Debug.LogError(message);
        }

        public static void LogError(object message, Object context)
        {
            Debug.LogError(message, context);
        }

        public static void LogErrorFormat(string message, params object[] args)
        {
            Debug.LogErrorFormat(message, args);
        }

        public static void LogErrorFormat(Object context, string message, params object[] args)
        {
            Debug.LogErrorFormat(context, message, args);
        }

        public static void LogException(System.Exception exception)
        {
            Debug.LogException(exception);
        }

        public static void LogException(System.Exception exception, Object context)
        {
            Debug.LogException(exception, context);
        }

        public static void Assert(bool condition)
        {
            Debug.Assert(condition);
        }

        public static void Assert(bool condition, Object context)
        {
            Debug.Assert(condition, context);
        }

        public static void AssertFormat(bool condition, string message, params object[] args)
        {
            Debug.AssertFormat(condition, message, args);
        }

        public static void AssertFormat(bool condition, Object context, string message, params object[] args)
        {
            Debug.AssertFormat(condition, context, message, args);
        }

#if UNITY_EDITOR
        [OnOpenAsset(0)]
        private static bool OnOpenDebugLog(int instance, int line)
        {
            string name = EditorUtility.InstanceIDToObject(instance).name;
            if (!name.Equals(nameof(LogUtilities))) return false;

            // 에디터 콘솔 윈도우의 인스턴스를 찾는다.
            var assembly = Assembly.GetAssembly(typeof(EditorWindow));
            if(assembly == null) return false;

            var consoleWindowType = assembly.GetType("UnityEditor.ConsoleWindow");
            if (consoleWindowType == null) return false;

            var consoleWindowField = consoleWindowType.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);
            if (consoleWindowField == null) return false;

            var consoleWindowInstance = consoleWindowField.GetValue(null);
            if (consoleWindowInstance == null) return false;

            if (consoleWindowInstance != (object)EditorWindow.focusedWindow) return false;

            // 콘솔 윈도우 인스턴스의 활성화된 텍스트를 찾는다.
            var activeTextField = consoleWindowType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
            if (activeTextField == null) return false;

            string activeTextValue = activeTextField.GetValue(consoleWindowInstance).ToString();
            if (string.IsNullOrEmpty(activeTextValue)) return false;

            // 디버그 로그를 호출한 파일 경로를 찾아 편집기로 연다.
            Match match = Regex.Match(activeTextValue, @"\(at (.+)\)");
            if (match.Success) match = match.NextMatch(); // stack trace의 첫번째를 건너뛴다.

            if (match.Success)
            {
                string path = match.Groups[1].Value;
                var split = path.Split(':');
                string drivePath = split[0];
                string filePath = split[1];
                int lineNum = Convert.ToInt32(split[2]);

                InternalEditorUtility.OpenFileAtLineExternal(drivePath + ':' + filePath, lineNum);
                return true;
            }
            return false;
        }
#endif
#else
        public static void Log(object message) {}
        public static void Log(object message, Object context) {}
        public static void LogFormat(string message, params object[] args) {}
        public static void LogFormat(Object context, string message, params object[] args) {}
        public static void LogWarning(object message) {}
        public static void LogWarning(object message, Object context) {}
        public static void LogWarningFormat(string message, params object[] args) {}
        public static void LogWarningFormat(Object context, string message, params object[] args) {}
        public static void LogError(object message) {}
        public static void LogError(object message, Object context) {}
        public static void LogErrorFormat(string message, params object[] args) {}
        public static void LogErrorFormat(Object context, string message, params object[] args) {}
        public static void LogException(System.Exception exception) {}
        public static void LogException(System.Exception exception, Object context) {}
        public static void Assert(bool condition) {}
        public static void Assert(bool condition, Object context) {}
        public static void AssertFormat(bool condition, string message, params object[] args) {}
        public static void AssertFormat(bool condition, Object context, string message, params object[] args) {}
#endif
    }
}
