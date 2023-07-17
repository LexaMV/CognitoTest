using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider.Model;
using System.Net;

// Manages all the text and button inputs
// Also acts like the main manager script for the game.
public class UIInputManager : MonoBehaviour
{
    public static string CachePath;

    public Button signupButton;
    public Button loginButton;
    public Button startButton;
    public Button logoutButton;
    public Button confirmEmail;
    public Button getIdGoods;

    public InputField emailFieldLogin;
    public InputField passwordFieldLogin;
    public InputField usernameField;
    public InputField emailField;
    public InputField passwordField;
    public InputField emailCode;

    private AuthenticationManager _authenticationManager;
    private GameObject _unauthInterface;
    private GameObject _authInterface;
    private LambdaManager _lambdaManager;
    private GameObject _loading;
    private GameObject _welcome;
    private GameObject _confirmEmail;
    private GameObject _signupContainer;

    private List<Selectable> _fields;
    private int _selectedFieldIndex = -1;

    private void displayComponentsFromAuthStatus(bool authStatus)
    {
        if (authStatus)
        {
            // Debug.Log("User authenticated, show welcome screen with options");
            _loading.SetActive(false);
            _unauthInterface.SetActive(false);
            _authInterface.SetActive(true);
            _welcome.SetActive(true);
        }
        else
        {
            // Debug.Log("User not authenticated, activate/stay on login scene");
            _loading.SetActive(false);
            _unauthInterface.SetActive(true);
            _authInterface.SetActive(false);
        }

        // clear out passwords
        passwordFieldLogin.text = "";
        passwordField.text = "";

        // set focus to email field on login form
        _selectedFieldIndex = -1;
    }

    private async void onLoginClicked()
    {
        _unauthInterface.SetActive(false);
        _loading.SetActive(true);
        // Debug.Log("onLoginClicked: " + emailFieldLogin.text + ", " + passwordFieldLogin.text);
        bool successfulLogin = await _authenticationManager.Login(emailFieldLogin.text, passwordFieldLogin.text);
        displayComponentsFromAuthStatus(successfulLogin);
    }

    private async void onSignupClicked()
    {
        _unauthInterface.SetActive(false);
        _loading.SetActive(true);

        // Debug.Log("onSignupClicked: " + usernameField.text + ", " + emailField.text + ", " + passwordField.text);
        bool successfulSignup = await _authenticationManager.Signup(usernameField.text, emailField.text, passwordField.text);

        if (successfulSignup)
        {
            // here we re-enable the whole auth container but hide the sign up panel
            _signupContainer.SetActive(false);

            _confirmEmail.SetActive(true);

            // copy over the new credentials to make the process smoother
            emailFieldLogin.text = emailField.text;
            passwordFieldLogin.text = passwordField.text;

            // set focus to email field on login form
            _selectedFieldIndex = 0;

        }
        else
        {
            _confirmEmail.SetActive(false);

            // set focus to email field on signup form
            _selectedFieldIndex = 3;
        }

        _loading.SetActive(false);
        _unauthInterface.SetActive(true);
    }

    private void onLogoutClick()
    {
        _authenticationManager.SignOut();
        displayComponentsFromAuthStatus(false);
    }

    private void onStartClick()
    {
        SceneManager.LoadScene("GameScene");
        Debug.Log("Changed to GameScene");

        // call to lambda to demonstrate use of credentials
        _lambdaManager.ExecuteLambda();
    }

    private async void RefreshToken()
    {
        bool successfulRefresh = await _authenticationManager.RefreshSession();
        displayComponentsFromAuthStatus(successfulRefresh);
    }

    void Start()
    {
        Debug.Log("UIInputManager: Start");
        // check if user is already authenticated 
        // We perform the refresh here to keep our user's session alive so they don't have to keep logging in.
        RefreshToken();

        signupButton.onClick.AddListener(onSignupClicked);
        loginButton.onClick.AddListener(onLoginClicked);
        startButton.onClick.AddListener(onStartClick);
        logoutButton.onClick.AddListener(onLogoutClick);
        confirmEmail.onClick.AddListener(onConfirmEmail);
    }

    private void onConfirmEmail()
    {
        SignUp();
    }

    //  IEnumerator ConfirmSignUp()
    //  {
    //      ConfirmSignUpSendData sendData = new ConfirmSignUpSendData();
    //      sendData.Username = usernameField.text;
    //      sendData.ConfirmationCode = emailCode.text;
    //      sendData.ClientId = "eu-central-1:a4a1fcd1-c7c4-4bc1-a1be-6872ee768ca5";

    //      byte[] bytePostData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(sendData));
    //      UnityWebRequest request = UnityWebRequest.Put(
    //    //   "https://cognito-idp.eu-central-1.amazonaws.com/"
    //      "https://cognito-idp.eu-central-1.amazonaws.com/eu-central-1_PpZfEUis8"


    //      , bytePostData);
    //      request.method = "POST";
    //      request.SetRequestHeader("Content-Type", "application/x-amz-json-1.1; charset=UTF-8");
    //      request.SetRequestHeader("X-Amz-Target", "AWSCognitoIdentityProviderService.ConfirmSignUp");

    //      yield return request.SendWebRequest();

    //      if (request.result == UnityWebRequest.Result.Success)
    //      {
    //          print(request.downloadHandler.text);
    //      }
    //      else
    //      {
    //          print(request.error);
    //      }
    //  }


    public async void SignUp()
    {
        bool confirmed = await _authenticationManager.ConfirmSignupAsync(emailCode.text, emailField.text);
    }

    void Update()
    {
        HandleInputTabbing();
    }

    // Handles tabbing between inputs and buttons
    private void HandleInputTabbing()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CheckForAndSetManuallyChangedIndex();

            // update index to where we need to tab to
            _selectedFieldIndex++;

            if (_selectedFieldIndex >= _fields.Count)
            {
                // reset back to first input
                _selectedFieldIndex = 0;
            }
            _fields[_selectedFieldIndex].Select();
        }
    }

    // If the user selects an input via mouse click, then the _selectedFieldIndex 
    // may not be accurate as the focused field wasn't change by tabbing. Here we
    // correct the _selectedFieldIndex in case they wish to start tabing from that point on.
    private void CheckForAndSetManuallyChangedIndex()
    {
        for (var i = 0; i < _fields.Count; i++)
        {
            if (_fields[i] is InputField && ((InputField)_fields[i]).isFocused && _selectedFieldIndex != i)
            {
                // Debug.Log("_selectedFieldIndex is : " + _selectedFieldIndex + ", Reset _selectedFieldIndex to: " + i);
                _selectedFieldIndex = i;
                break;
            }
        }
    }

    void Awake()
    {
        CachePath = Application.persistentDataPath;

        _unauthInterface = GameObject.Find("UnauthInterface");
        _authInterface = GameObject.Find("AuthInterface");
        _loading = GameObject.Find("Loading");
        _welcome = GameObject.Find("Welcome");
        _confirmEmail = GameObject.Find("ConfirmEmail");
        _signupContainer = GameObject.Find("SignupContainer");

        _unauthInterface.SetActive(false); // start as false so we don't just show the login screen during attempted token refresh
        _authInterface.SetActive(false);
        _welcome.SetActive(false);
        _confirmEmail.SetActive(false);
        _signupContainer.SetActive(true);

        _authenticationManager = FindObjectOfType<AuthenticationManager>();
        _lambdaManager = FindObjectOfType<LambdaManager>();

        _fields = new List<Selectable> { emailFieldLogin, passwordFieldLogin, loginButton, emailField, usernameField, passwordField, signupButton };
    }

    [System.Serializable]
    public class ConfirmSignUpSendData
    {
        public string Username;
        public string ConfirmationCode;
        public string ClientId;
    }
}


