using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour
{
    public static AuthenticationManager Instance { get; private set; }

    public event Action OnSignedIn;
    public event Action<string> OnSignInFailed;

    public string PlayerId { get; private set; }

    private async void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            await InitializeUnityServices();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Services Initialized");
            
            AuthenticationService.Instance.SignedIn += () =>
            {
                PlayerId = AuthenticationService.Instance.PlayerId;
                Debug.Log($"Signed in as: {PlayerId}");
                OnSignedIn?.Invoke();
            };

            AuthenticationService.Instance.SignInFailed += (err) =>
            {
                Debug.LogError($"Sign in failed: {err}");
                OnSignInFailed?.Invoke(err.ToString());
            };

            await SignInAnonymously();
        }
        catch (Exception e)
        {
            Debug.LogError($"Unity Services Initialization failed: {e}");
            OnSignInFailed?.Invoke(e.Message);
        }
    }

    public async Task SignInAnonymously()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            OnSignedIn?.Invoke();
            return;
        }

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Authentication Exception: {ex}");
            OnSignInFailed?.Invoke(ex.Message);
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"Request Failed Exception: {ex}");
            OnSignInFailed?.Invoke(ex.Message);
        }
    }
}
