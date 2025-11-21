// UGSAuthenticator.cs
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;

public class UGSAuthenticator : MonoBehaviour
{
    async void Awake()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Successfully signed in. Player ID: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize or sign in: {e}");
        }
    }
}