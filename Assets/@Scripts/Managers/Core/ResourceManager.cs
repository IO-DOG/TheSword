using DG.Tweening.Plugins.Core.PathCore;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

public class ResourceManager
{
    // 실제 로드한 리소스.
    Dictionary<string, UnityEngine.Object> _resources = new Dictionary<string, UnityEngine.Object>();
    // 로드 중인 키. 같은 키를 두 번 부르면 두 번째는 첫 요청의 결과를 기다린다.
    Dictionary<string, Action<Object>> _pending = new Dictionary<string, Action<Object>>();

    #region 리소스 로드
    public T Load<T>(string key) where T : Object
    {
        if (_resources.TryGetValue(key, out Object resource))
        {
            return resource as T;
        }

        //스프라이트 로드할때 항상 .sprite가 붙어 있어야하는데 데이터시트에 .sprite가 붙어있지 않은 데이터가 많음
        //임시로 붙임 -드래곤
        if (typeof(T) == typeof(Sprite))
        {
            key = key + ".sprite";
            if (_resources.TryGetValue(key, out Object temp))
            {
                return temp as T;
            }
        }

        //if (typeof(T) == typeof(SpriteAtlas))
        //{
        //    key = key + ".spriteatlas";
        //    if (_resources.TryGetValue(key, out Object temp))
        //    {
        //        return temp as T;
        //    }
        //}

        return null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 배치모드 검증용. Addressables 를 초기화하지 않고 캐시를 직접 채운다.
    /// (헤드리스에서는 LoadAsync 의 완료 콜백이 돌지 않아 리소스를 못 받는다)
    /// </summary>
    public void EditorPreload(string key, Object obj)
    {
        if (obj != null && _resources.ContainsKey(key) == false)
            _resources.Add(key, obj);
    }
#endif

    public GameObject Instantiate(string key, Transform parent = null, bool pooling = false)
    {
        GameObject prefab = Load<GameObject>($"{key}");
        if (prefab == null)
        {
            Debug.LogError($"Failed to load prefab : {key}");
            return null;
        }

        GameObject go;
        if (pooling)
        {
            go = Managers.Pool.Pop(prefab);
        }
        else
        {
            go = Object.Instantiate(prefab, parent);
            go.name = prefab.name;
        }

        // 프리팹이 스스로 트는 소리도 효과음 슬라이더를 따른다. 만드는 곳(풀 포함)이 여기 하나라 여기서 한 번.
        Managers.Sound?.FollowEffectVolume(prefab, go);
        return go;
    }

    public void Destroy(GameObject go, float time = 0.0f)
    {
        if (go == null)
            return;

        //if (Managers.Pool.Push(go))
        //    return;

        Object.Destroy(go, time);
    }

    #endregion
    #region 어드레서블

    public void LoadAsync<T>(string key, Action<T> callback = null) where T : Object
    {
        if (_resources.TryGetValue(key, out Object cached) && cached != null)
        {
            callback?.Invoke(cached as T);
            return;
        }
        Action<Object> listener = obj => callback?.Invoke(obj as T);
        if (_pending.ContainsKey(key)) { _pending[key] += listener; return; }
        _pending.Add(key, listener);
        string loadKey = key.EndsWith(".sprite", StringComparison.Ordinal)
            ? $"{key}[{key.Substring(0, key.Length - 7)}]" : key;
        var request = Addressables.LoadAssetAsync<T>(loadKey);
        request.Completed += op => {
            Object result = null;
            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                result = op.Result;
                _resources[key] = result;
            }
            else
            {
                Debug.LogWarning($"[Resource] Failed to load {key}: {op.OperationException}");
                Addressables.Release(op);
            }
            var listeners = _pending[key];
            _pending.Remove(key);
            listeners?.Invoke(result);
        };
    }

    // Completion is separate from progress: reaching 100% does not imply success.
    public void LoadAllAsync<T>(string label, Action<string, int, int> progress,
        Action<string> completed = null) where T : Object
    {
        var request = Addressables.LoadResourceLocationsAsync(label, typeof(T));
        request.Completed += op => {
            if (op.Status != AsyncOperationStatus.Succeeded || op.Result == null || op.Result.Count == 0)
            {
                string error = $"No loadable resources for label '{label}': {op.OperationException}";
                Addressables.Release(op);
                completed?.Invoke(error);
                return;
            }
            var keys = op.Result.Select(location => location.PrimaryKey).Distinct().ToArray();
            Addressables.Release(op);
            int count = 0;
            var failures = new List<string>();
            foreach (string key in keys)
            {
                void OnLoaded(Object obj)
                {
                    if (obj == null) failures.Add(key);
                    count++;
                    progress?.Invoke(key, count, keys.Length);
                    if (count == keys.Length)
                        completed?.Invoke(failures.Count == 0 ? null : "Missing resources: " + string.Join(", ", failures));
                }
                if (key.EndsWith(".sprite", StringComparison.Ordinal)) LoadAsync<Sprite>(key, OnLoaded);
                else LoadAsync<T>(key, OnLoaded);
            }
        };
    }
    #endregion
}
