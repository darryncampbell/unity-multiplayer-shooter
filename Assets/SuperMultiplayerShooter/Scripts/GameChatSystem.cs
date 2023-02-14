﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Chat;
using Photon.Pun;
using Photon.Realtime;
using PubNubAPI;
using System;
using UnityEditor.ShaderGraph.Internal;
using System.Reflection;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using Newtonsoft.Json;

namespace Visyde
{
    /// <summary>
    /// Chat System (Lobby in main menu only)
    /// - manages the chat system itself as well as the chat's UI in one script
    /// </summary>

    public class GameChatSystem : MonoBehaviour
    {
        [Header("Settings:")]
        public Color ourColor;
        public Color othersColor;
        public Color ourChatColor;
        public Color othersChatColor;
        public Color positiveNotifColor;
        public Color negativeNotifColor;
        [Header("References:")]
        public Text messageDisplay;
        public InputField inputField;
        public Button sendButton;
        public GameObject loadingIndicator;
     
        // Internals:
        VerticalLayoutGroup vlg;

        //Use the host user id as part of the channel name once joined a room to join a unique channel room.
        string gameChannel = "-game-chat";

        // Use this for initialization
        void Start()
        {
            vlg = messageDisplay.transform.parent.GetComponent<VerticalLayoutGroup>();
            loadingIndicator.SetActive(false);
            messageDisplay.text = "";
            gameChannel = !String.IsNullOrWhiteSpace(PhotonNetwork.MasterClient.UserId) ? PhotonNetwork.MasterClient.UserId + gameChannel : gameChannel;

            /*
            PNConfiguration pnConfiguration = new PNConfiguration();
            pnConfiguration.SubscribeKey = "sub-c-68a629b6-9566-4f83-b74b-000b0fad8a69";
            pnConfiguration.PublishKey = "pub-c-27c9a7a7-bbb0-4210-8dd7-850512753b31";
            pnConfiguration.LogVerbosity = PNLogVerbosity.BODY;
            pnConfiguration.UserId = SystemInfo.deviceUniqueIdentifier; //Guarenteed to be unique for every device.
            */
            //TODO: Rework so don't have to reinitialize the object.
            //Reinitialize PubNub object due to new scene change.
            PubNubManager.PubNub = new PubNub(PubNubManager.PubNub.PNConfig);


            //Listen for any new incoming messages
            PubNubManager.PubNub.SubscribeCallback += (sender, e) => {
                SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;
                if (mea.MessageResult != null)
                {
                    //Extracts username and displays user plus chat.
                    GetUsername(mea.MessageResult);
                }
                if (mea.PresenceEventResult != null)
                {
                    Debug.Log("In Example, SubscribeCallback in presence" + mea.PresenceEventResult.Channel + mea.PresenceEventResult.Occupancy + mea.PresenceEventResult.Event);
                }
            };

            //Subscribe to the lobby chat channel
            PubNubManager.PubNub.Subscribe()
               .Channels(new List<string>(){
                    gameChannel
               })
               .Execute();
        }

        // Update is called once per frame
        void Update()
        {
            //Can always check to see if pubnub connection still active.
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SendChatMessage();
                inputField.ActivateInputField();
            }
        }

        //Extracts the username from the metadata to display in chat.
        private void GetUsername(PNMessageResult message)
        {
            //Pull out name entered in app.
            string username = "Guest";
            var metaDataJSON = JsonConvert.SerializeObject(message.UserMetadata);

            var metaDataDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(metaDataJSON);
            //If cannot find the metadata, grab first six chars of user id. If the user id is also blank, set back-up as guest.
            if (metaDataDictionary == null || !metaDataDictionary.TryGetValue("name", out username))
            {
                username = !String.IsNullOrWhiteSpace(message.IssuingClientId) ? message.IssuingClientId.Substring(0, 10) : "Guest";
            }
            //Display the chat pulled.
            DisplayChat(message.Payload.ToString(), username, PubNubManager.PubNub.PNConfig.UserId.Equals(message.IssuingClientId), false, false);
        }

        public void SendChatMessage(){
            if (!string.IsNullOrEmpty(inputField.text))
            {             
                //Dictionary to store metadata (username)
                Dictionary<string, string> metaDict = new Dictionary<string, string>();
                metaDict.Add("name", PlayerPrefs.GetString("name")); //associate the name entered by user with the id.
                //PubNub Publish              
                PubNubManager.PubNub.Publish()
                    .Channel(gameChannel)
                    .Message(inputField.text)
                    .Meta(metaDict)
                    .Async((result, status) => {
                        if (!status.Error)
                        {
                            Debug.Log(string.Format("DateTime {0}, In Publish Example, Timetoken: {1}", DateTime.UtcNow, result.Timetoken));
                        }
                        else
                        {
                            Debug.Log(status.Error);
                            Debug.Log(status.ErrorData.Info);
                        }
                    });



                inputField.text = string.Empty;
            }
        }
        public void SendSystemChatMessage(string message, bool negative){
            DisplayChat(message, "", false, true, negative);
        }
        void DisplayChat(string message, string from, bool ours, bool systemMessage, bool negative){

            string finalMessage = "";
            
            if (systemMessage){
                finalMessage = "\n" + "<color=#" + ColorUtility.ToHtmlStringRGBA(negative ? negativeNotifColor : positiveNotifColor) + ">" + message + "</color>";
            }
            else{
                finalMessage = "\n" + "<color=#" + ColorUtility.ToHtmlStringRGBA(ours ? ourColor : othersColor) + ">" + from + "</color>: <color=" + ColorUtility.ToHtmlStringRGBA(ours ? ourChatColor : othersChatColor) + ">" + message + "</color>";
            }
            messageDisplay.text += finalMessage;

            // Canvas refresh:
            Canvas.ForceUpdateCanvases();
            vlg.enabled = false;
            vlg.enabled = true;
        }

       
        void OnLeftRoom()
        {         
            //Unsubscrube from lobby chat once player leaves the room.
            PubNubManager.PubNub.Unsubscribe()
                .Channels(new List<string>()
                {
                    gameChannel
                })
                .Async((result, status) =>
                {
                    if (status.Error)
                    {
                        Debug.Log(string.Format("Unsubscribe Error: {0} {1} {2}", status.StatusCode, status.ErrorData, status.Category));
                    }
                    else
                    {
                        Debug.Log(string.Format("DateTime {0}, In Unsubscribe, result: {1}", DateTime.UtcNow, result.Message));
                    }
                });
        }
        public void OnChatStateChange(ChatState state) { }
        public void OnStatusUpdate(string user, int status, bool gotMessage, object message){}       
        public void OnPrivateMessage(string sender, object message, string channelName) { }
        public void OnUserSubscribed(string channel, string user) { }
        public void OnUserUnsubscribed(string channel, string user) { }             
        public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string message)
        {
            if (level == ExitGames.Client.Photon.DebugLevel.ERROR)
            {
                UnityEngine.Debug.LogError(message);
            }
            else if (level == ExitGames.Client.Photon.DebugLevel.WARNING)
            {
                UnityEngine.Debug.LogWarning(message);
            }
            else
            {
                UnityEngine.Debug.Log(message);
            }
        }
    }
}