using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eu4ng.Utilities
{
    [Serializable]
    public class SerializableDictionary<TK, TV> : Dictionary<TK, TV>, ISerializationCallbackReceiver
    {
        [SerializeField] List<TK> m_Keys = new List<TK>();

        [SerializeField] List<TV> m_Values = new List<TV>();

        List<int> m_DuplicatedIndices = new List<int>();

        List<TK> m_DuplicatedKeys = new List<TK>();

        List<TV> m_DuplicatedValues = new List<TV>();

        public void OnBeforeSerialize()
        {
            // 초기화
            m_Keys.Clear();
            m_Values.Clear();

            // Dictionary에 등록된 Key, Value 가져오기
            var dictionaryKeys = new List<TK>();
            var dictionaryValues = new List<TV>();
            foreach (var pair in this)
            {
                dictionaryKeys.Add(pair.Key);
                dictionaryValues.Add(pair.Value);
            }

            // m_Keys, m_Values 갱신
            int duplicatedIndex = 0;
            int dictionaryIndex = 0;
            for (int i = 0; i < dictionaryKeys.Count + m_DuplicatedKeys.Count; ++i)
            {
                if (m_DuplicatedIndices.Count > duplicatedIndex && m_DuplicatedIndices[duplicatedIndex] == i)
                {
                    // Dictionary에 등록되지 않은 중복된 Key, Value 추가
                    m_Keys.Add(m_DuplicatedKeys[duplicatedIndex]);
                    m_Values.Add(m_DuplicatedValues[duplicatedIndex]);
                    ++duplicatedIndex;
                }
                else
                {
                    // Dictionary에 등록된 Key, Value 추가
                    m_Keys.Add(dictionaryKeys[dictionaryIndex]);
                    m_Values.Add(dictionaryValues[dictionaryIndex]);
                    ++dictionaryIndex;
                }
            }
        }

        public void OnAfterDeserialize()
        {
            // 초기화
            Clear();
            m_DuplicatedIndices.Clear();
            m_DuplicatedKeys.Clear();
            m_DuplicatedValues.Clear();

            // Key, Value 목록을 Dictionary에 추가
            for (int i = 0; i < m_Keys.Count; ++i)
            {
                var key = m_Keys[i];
                var value = m_Values.Count > i ? m_Values[i] : default;

                // Key가 중복되는 경우 Dictionary 대신 Duplicated Key, Value 목록에 추가
                if (!TryAdd(key, value))
                {
                    m_DuplicatedIndices.Add(i);
                    m_DuplicatedKeys.Add(key);
                    m_DuplicatedValues.Add(value);
                }
            }
        }
    }
}
