using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameRemoteAPI
{
    private const string BASE_URL = "http://localhost:3000";

    // 当前登录用户的 token
    public static string AuthToken { get; private set; }

    // 清除 token（退出登录）
    public static void ClearToken()
    {
        AuthToken = null;
    }

    // 注册账号
    public static IEnumerator Register(string username, string password, Action<bool, string> callback)
    {
        var formData = new Dictionary<string, string>
        {
            { "username", username },
            { "password", password }
        };

        using (var www = UnityWebRequest.Post(BASE_URL + "/register", formData))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<BaseResponse>(www.downloadHandler.text);
                if (response.code == 0)
                {
                    callback(true, response.message);
                }
                else
                {
                    callback(false, response.message);
                }
            }
            else
            {
                callback(false, www.error);
            }
        }
    }

    // 登录
    public static IEnumerator Login(string username, string password, Action<bool, LoginResult> callback)
    {
        var formData = new Dictionary<string, string>
        {
            { "username", username },
            { "password", password }
        };

        using (var www = UnityWebRequest.Post(BASE_URL + "/login", formData))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<LoginResponse>(www.downloadHandler.text);
                if (response.code == 0)
                {
                    // 保存 token
                    AuthToken = response.token;
                    callback(true, new LoginResult { userId = response.userId, username = username, token = response.token });
                }
                else
                {
                    callback(false, null);
                }
            }
            else
            {
                callback(false, null);
            }
        }
    }

    // 获取服务器列表
    public static IEnumerator GetServers(Action<List<Server>> callback)
    {
        using (var www = UnityWebRequest.Get(BASE_URL + "/servers"))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<ServerListResponse>(www.downloadHandler.text);
                callback(response.servers);
            }
            else
            {
                callback(null);
            }
        }
    }

    // 进入服务器（需要登录）
    public static IEnumerator EnterServer(int userId, int serverId, Action<PlayerServerData> callback)
    {
        var formData = new Dictionary<string, string>
        {
            { "userId", userId.ToString() },
            { "serverId", serverId.ToString() }
        };

        using (var www = UnityWebRequest.Post(BASE_URL + "/enter_server", formData))
        {
            // 添加登录 token
            www.SetRequestHeader("X-Auth-Token", AuthToken);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<PlayerServerData>(www.downloadHandler.text);
                callback(response);
            }
            else
            {
                callback(null);
            }
        }
    }

    // 创建角色（需要登录）
    public static IEnumerator CreateRole(int userId, int serverId, string name, Action<bool, string> callback)
    {
        var json = JsonUtility.ToJson(new CreateRoleRequest
        {
            userId = userId,
            serverId = serverId,
            name = name
        });

        using (var www = UnityWebRequest.Post(BASE_URL + "/create_role", json, "application/json"))
        {
            // 添加登录 token
            www.SetRequestHeader("X-Auth-Token", AuthToken);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<BaseResponse>(www.downloadHandler.text);
                if (response.code == 0)
                {
                    callback(true, response.message);
                }
                else
                {
                    callback(false, response.message);
                }
            }
            else
            {
                callback(false, www.error);
            }
        }
    }

    // 查询玩家信息（需要登录）
    public static IEnumerator GetPlayerInfo(int userId, int serverId, Action<PlayerServerData> callback)
    {
        var formData = new Dictionary<string, string>
        {
            { "userId", userId.ToString() },
            { "serverId", serverId.ToString() }
        };

        using (var www = UnityWebRequest.Post(BASE_URL + "/player_info", formData))
        {
            // 添加登录 token
            www.SetRequestHeader("X-Auth-Token", AuthToken);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<PlayerServerData>(www.downloadHandler.text);
                callback(response);
            }
            else
            {
                callback(null);
            }
        }
    }

    // 查询背包（需要登录）
    public static IEnumerator GetBag(int userId, int serverId, Action<List<Item>> callback)
    {
        var formData = new Dictionary<string, string>
        {
            { "userId", userId.ToString() },
            { "serverId", serverId.ToString() }
        };

        using (var www = UnityWebRequest.Post(BASE_URL + "/bag", formData))
        {
            // 添加登录 token
            www.SetRequestHeader("X-Auth-Token", AuthToken);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<ItemListResponse>(www.downloadHandler.text);
                callback(response.items);
            }
            else
            {
                callback(null);
            }
        }
    }

    // 增加金币（需要登录）
    public static IEnumerator AddGold(int userId, int serverId, int gold, Action<bool, string> callback)
    {
        var json = JsonUtility.ToJson(new AddGoldRequest
        {
            userId = userId,
            serverId = serverId,
            gold = gold
        });

        using (var www = UnityWebRequest.Post(BASE_URL + "/add_gold", json, "application/json"))
        {
            // 添加登录 token
            www.SetRequestHeader("X-Auth-Token", AuthToken);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<BaseResponse>(www.downloadHandler.text);
                if (response.code == 0)
                {
                    callback(true, response.message);
                }
                else
                {
                    callback(false, response.message);
                }
            }
            else
            {
                callback(false, www.error);
            }
        }
    }

    // 添加物品（需要登录）
    public static IEnumerator AddItem(int userId, int serverId, int itemId, string name, int count, Action<bool, string> callback)
    {
        var json = JsonUtility.ToJson(new AddItemRequest
        {
            userId = userId,
            serverId = serverId,
            itemId = itemId,
            name = name,
            count = count
        });

        using (var www = UnityWebRequest.Post(BASE_URL + "/add_item", json, "application/json"))
        {
            // 添加登录 token
            www.SetRequestHeader("X-Auth-Token", AuthToken);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<BaseResponse>(www.downloadHandler.text);
                if (response.code == 0)
                {
                    callback(true, response.message);
                }
                else
                {
                    callback(false, response.message);
                }
            }
            else
            {
                callback(false, www.error);
            }
        }
    }

    // ========== 数据模型 ==========

    [Serializable]
    public class Account
    {
        public int id;
        public string username;
        public string password;
    }

    [Serializable]
    public class Server
    {
        public int id;
        public string name;
        public string status;
        public int online;
        public bool isNew;
    }

    [Serializable]
    public class Item
    {
        public int id;
        public string name;
        public int count;
    }

    [Serializable]
    public class PlayerServerData
    {
        public int userId;
        public int serverId;
        public string name;
        public int level;
        public int gold;
        public List<Item> items;
    }

    [Serializable]
    public class BaseResponse
    {
        public int code;
        public string message;
    }

    [Serializable]
    public class LoginResponse
    {
        public int code;
        public string message;
        public int userId;
        public string token;
    }

    [Serializable]
    public class LoginResult
    {
        public int userId;
        public string username;
        public string token;
    }

    [Serializable]
    public class ServerListResponse
    {
        public List<Server> servers;
    }

    [Serializable]
    public class ItemListResponse
    {
        public List<Item> items;
    }

    [Serializable]
    public class CreateRoleRequest
    {
        public int userId;
        public int serverId;
        public string name;
    }

    [Serializable]
    public class AddGoldRequest
    {
        public int userId;
        public int serverId;
        public int gold;
    }

    [Serializable]
    public class AddItemRequest
    {
        public int userId;
        public int serverId;
        public int itemId;
        public string name;
        public int count;
    }
}
