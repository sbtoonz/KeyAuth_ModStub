using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KeyAuth;
using ServerSync;
using UnityEngine;
using UnityEngine.Networking;

namespace NewMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class NewMod : BaseUnityPlugin
    {
        internal const string ModName = "NewMod";
        internal const string ModVersion = "0.0.1";
        private const string ModGUID = "some.new.mod";
        private static Harmony harmony = null!;

        #region  ServerSync ConfigSync

        ConfigSync configSync = new(ModGUID) 
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion};
        internal static ConfigEntry<bool> ServerConfigLocked = null!;
        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }
        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);


        #endregion
 
        private ConfigEntry<string> _username; //comment this out if you want to use license instead of username/pass
        private ConfigEntry<string> _password; //comment this out if you want to use license instead of username/pass
        //private ConfigEntry<string> _license; //Uncomment if you want to use a license instead of a username/pass

        private static KeyAuthAPI _API = new(
            name: "",
            ownerid: "",
            secret: "",
            version: "1.0"
        );
        
        public void Awake()
        {
            ServerConfigLocked = config("1 - General", "Lock Configuration", true, "If on, the configuration is locked and can be changed by server admins only.", true);
            _username = config("2 - KeyAuth", "Username", "", "Put your username provided to you by mod author here", false); //Comment this out and uncomment license if youd like to use the a license instead of a username/pass
            _password = config("2 - KeyAuth", "Password", "", "Put your password provided to you by mod author here", false); //Comment this out and uncomment license if youd like to use the a license instead of a username/pass
            //_license = config("2 - KeyAuth", "License", "", "Put the license provided to you by mod author here"); //Uncomment if you want to use a license instead of a username/pass

            if (_API.ownerid.IsNullOrWhiteSpace())
            {
                Debug.LogError("Please check your setup the API section is not filled out properly");
                return;
            }
            
            _API.Init();
            if(!_API.response.success)return;
            _API.login(_username.Value, _password.Value); //Comment this out and uncomment license if youd like to use the a license instead of a username/pass
            //_API.license(_license.Value); //Uncomment if you want to use a license instead of a username/pass
            if (_API.response.success)
            {
                _API.log("User Login"); //Send Discord WebHook
                
                Assembly assembly = Assembly.GetExecutingAssembly();
                harmony = new(ModGUID);
                harmony.PatchAll(assembly);
                configSync.AddLockingConfigEntry(ServerConfigLocked);
                
                StartCoroutine(KeyAuthAPI.CheckKeyAuth(_API)); //This will check the session every 3s without this you can only check session on login and the session will quickly expire
            }
            else
            {
                Debug.LogWarning($"Failed to login please check credentials {_API.response.message}");
            }
        }
    }
}
