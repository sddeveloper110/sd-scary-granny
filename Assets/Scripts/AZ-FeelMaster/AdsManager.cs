using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using System.Collections;

public enum AdLoadingStatus
{
    NotLoaded,
    Loading,
    Loaded,
    NoInventory
}
public enum AdType
{
    NoAds,
    Ads
}

public enum BannerSize
{
    Small,
    Smart,
    LeaderBoard,
    Adaptive
};

public enum BannerPos
{
    Top,
    TopLeft,
    TopRight,
    Bottom,
    BottomLeft,
    BottomRight
};

[Serializable]
public class AdmobId
{
    [Header("BannerAd")]
    public string ADMOB_BANNER_AD_ID;
    [Space]
    public string ADMOB_BANNER_AD_ID_2;

    [Header("MediumBannerAd")]
    public string ADMOB_MEDIUM_BANNER_AD_ID;

    [Header("InterAd")]
    public string ADMOB_INTERTITIAL_AD_ID;

    [Header("RewardedAd")]
    public string ADMOB_REWARDED_AD_ID;

    [Header("AppOpenAd")]
    public string ADMOB_AppOpen_AD_ID;
    public string ADMOB_AppOpen_AD_ID_2;
}

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;
    [Space]
    [Header("Google Admob Network Setting")]
    public AdmobId ADMOB_ID = new AdmobId();
    [Space]

    [Header("Banner Ads Setting")]
    public bool isShowBanner = false;
    public bool isDoubleBannerBanner = false;


    public static bool OppenApp_Not_Shown;
    public delegate void RewardUserDelegate();
    private static RewardUserDelegate NotifyReward;
    private static DateTime TimeForAppOpenAds;
    private static DateTime TimeForInterAds;
    public static bool firstOpen = true;
    public static bool firstOpen2 = true;
    public static bool interReward = false;

    private bool isBannerShowing;
    private bool isBanner2Showing;
    private bool isMRecShowing;
    private int interstitialRetryAttempt;
    private int rewardedRetryAttempt;
    private int appOpenChainCount = 0;


    //................................
    public GameObject NoAdLoaded;
    public GameObject loadAdPanel;

    [Header("Google Admob Network Setting")]
    public bool isAdmobBanner = true;
    public bool isAdmobMedium = true;
    public bool isAdmobInter = true;
    public bool isAdmobRewarded = true;
    public bool isAdmobAppOpen = true;

    public bool isRemoteAdsEnabled = true;

    public static AdType adsStatus;
    public static AdLoadingStatus smallBannerStatus = AdLoadingStatus.NotLoaded;
    public static AdLoadingStatus smallBanner2Status = AdLoadingStatus.NotLoaded;
    public static AdLoadingStatus mediumBannerStatus = AdLoadingStatus.NotLoaded;
    public static AdLoadingStatus iAdStatus = AdLoadingStatus.NotLoaded;
    public static AdLoadingStatus rAdStatus = AdLoadingStatus.NotLoaded;

    AdPosition AdmobBannerPos;
    AdPosition AdmobBanner2Pos;
    [Header("Banner Setting")]
    public BannerPos _AdmobBannerPos;
    public BannerPos _AdmobBanner2Pos;
    public BannerSize _AdmobBannerSize;
    public BannerSize _AdmobBanner2Size;
    AdSize bannerSize;
    AdSize banner2Size;
    [Space]
    [Header("Medium Banner Setting")]
    public AdPosition _AdmobMediumBannerPos;
    [Space]
    bool isAdmobInitialized = false, isAdmobBannerInitialized = false, isAdmobBanner2Initialized = false;

    #region Banner
    private BannerView banner;
    public static bool isSmallBannerLoaded = false;
    #endregion

    #region Banner2
    private BannerView banner2;
    public static bool isSmallBanner2Loaded = false;
    #endregion

    #region MediumBanner
    private BannerView mediumbanner;
    public static bool isMediumBannerLoaded = false;
    #endregion

    #region Interstitial
    private InterstitialAd interstitialAd;
    #endregion

    #region RewardedAd
    private RewardedAd rewardedAd;
    #endregion

    #region AppOpenAd
    [HideInInspector]
    private AppOpenAd appOpenAd;

    private readonly TimeSpan APPOPEN_TIMEOUT = TimeSpan.FromHours(4);
    private DateTime appOpenExpireTime;

    #endregion

    #region AppOpenAd2
    [HideInInspector]
    private AppOpenAd appOpenAd2;

    private readonly TimeSpan APPOPEN_TIMEOUT2 = TimeSpan.FromHours(4);
    private DateTime appOpenExpireTime2;

    #endregion

    [Header("Ad Break Settings")]
    public GameObject adBreakPanel;
    public TMPro.TextMeshProUGUI adBreakCountdownTxt;
    private Coroutine adBreakRoutine;
    private float adBreakInterval = 120f;
    private float adBreakCountdownTime = 10f;

    #region Unity Functions
    void Awake()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        if (Instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        if (SystemInfo.systemMemorySize > 1024)
        {
            Debug.Log("MemoryIsGreaterThen 1024");
            adsStatus = AdType.Ads;
        }
        else
        {
            Debug.Log("MemoryIsNotGreaterThen 1024");
            adsStatus = AdType.NoAds;
        }


        switch (_AdmobBannerSize)
        {
            case BannerSize.Small:
                bannerSize = AdSize.Banner;
                break;
            case BannerSize.Smart:
                bannerSize = AdSize.SmartBanner;
                break;
            case BannerSize.LeaderBoard:
                bannerSize = AdSize.Leaderboard;
                break;
            case BannerSize.Adaptive:
                AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                bannerSize = adaptiveSize;
                break;
        }
        switch (_AdmobBanner2Size)
        {
            case BannerSize.Small:
                banner2Size = AdSize.Banner;
                break;
            case BannerSize.Smart:
                banner2Size = AdSize.SmartBanner;
                break;
            case BannerSize.LeaderBoard:
                banner2Size = AdSize.Leaderboard;
                break;
            case BannerSize.Adaptive:
                AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                banner2Size = adaptiveSize;
                break;
        }
        SetBannerPos(_AdmobBannerPos);
        SetBanner2Pos(_AdmobBanner2Pos);
    }
    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitForSeconds(2f);
        OppenApp_Not_Shown = false;
        interReward = false;
        Initialize_Admob_Ads();

        GameManager.OnGameStarted += StartAdBreakRoutine;
    }

    private void OnDestroy()
    {
        GameManager.OnGameStarted -= StartAdBreakRoutine;
    }

    void EnableNoAdLoaded()
    {
        if (NoAdLoaded.activeInHierarchy)
        {
            //CancelInvoke(nameof(DisanleNoAd));
            NoAdLoaded.SetActive(false);
        }
        NoAdLoaded.SetActive(true);
        //Invoke(nameof(DisanleNoAd), 1f);
    }

    void DisanleNoAd()
    {
        NoAdLoaded.SetActive(false);
        CancelInvoke(nameof(DisanleNoAd));
    }

    private void OnDisable()
    {
        DestroyBannerAd();
        DestroyBanner2Ad();
        smallBannerStatus = AdLoadingStatus.NotLoaded;
        smallBanner2Status = AdLoadingStatus.NotLoaded;
        isSmallBannerLoaded = false;
        isSmallBanner2Loaded = false;
    }

    #endregion

    #region Admob Network

    #region Initialize Admob

    public void Initialize_Admob_Ads()
    {
        print("going in");
        Debug.Log("RemoteConfig Data" + isRemoteAdsEnabled);
        MobileAds.Initialize((initStatus) =>
        {
            Debug.Log("Admob_Initialized");

            Dictionary<string, AdapterStatus> map = initStatus.getAdapterStatusMap();
            foreach (KeyValuePair<string, AdapterStatus> keyValuePair in map)
            {
                string className = keyValuePair.Key;
                AdapterStatus status = keyValuePair.Value;
                switch (status.InitializationState)
                {
                    case AdapterState.NotReady:
                        // The adapter initialization did not complete.
                        Debug.Log("Adapter: " + status.Description + " not ready :" + className);
                        break;
                    case AdapterState.Ready:
                        // The adapter was successfully initialized.
                        Debug.Log("Adapter: " + className + " is initialized.");
#if UNITY_ANDROID
                        MediationAdapterConsent(className);
#endif

#if UNITY_IOS
                        MediationAdapterConsent(className);
#endif

                        break;
                }
            }

#if UNITY_EDITOR
            isAdmobInitialized = true;
            isAdmobBannerInitialized = true;
            if (isAdmobInitialized)
            {
                if (PlayerPrefs.GetInt("RemoveAds") != 1)
                {
                    Debug.Log("calls");
                    if (isAdmobBanner)
                    {
                        Invoke(nameof(LoadAdmobSmallBanner), 3.5f);
                        if (isDoubleBannerBanner)
                        {
                            Invoke(nameof(LoadAdmobSmallBanner2), 3.5f);
                        }
                    }
                    if (isAdmobAppOpen)
                    {
                        LoadAdmobAppOpenAd();
                        LoadAdmobAppOpenAd2();
                    }
                    if (isAdmobMedium)
                    {
                        LoadAdmobMediumBanner();
                    }
                    if (isAdmobInter)
                    {
                        LoadAdmobInterstitial();
                    }
                }

                if (isAdmobRewarded)
                {
                    LoadAdmobRewardedVideo();
                }
            }
#endif
        });

#if UNITY_IOS
        MobileAds.SetiOSAppPauseOnBackground(true);
#endif
    }

    void MediationAdapterConsent(string AdapterClassname)
    {
        if (AdapterClassname.Contains("MobileAds"))
        {
            Debug.Log("consent is send");
            isAdmobInitialized = true;
            isAdmobBannerInitialized = true;
            if (isAdmobInitialized)
            {
                if (PlayerPrefs.GetInt("RemoveAds") != 1)
                {
                    Debug.Log("Load calls");
                    if (isAdmobBanner)
                    {
                        LoadAdmobSmallBanner();
                        if (isDoubleBannerBanner)
                        {
                            LoadAdmobSmallBanner2();
                        }
                    }
                    if (isAdmobAppOpen)
                    {
                        LoadAdmobAppOpenAd();
                        LoadAdmobAppOpenAd2();
                    }
                    if (isAdmobMedium)
                    {
                        LoadAdmobMediumBanner();
                    }
                    if (isAdmobInter)
                    {
                        LoadAdmobInterstitial();

                    }
                }

                if (isAdmobRewarded)
                {
                    LoadAdmobRewardedVideo();
                }
            }
        }
    }

    public void SetBannerPos(BannerPos Pos)
    {
        switch (Pos)
        {
            case BannerPos.Top:
                AdmobBannerPos = AdPosition.Top;
                break;
            case BannerPos.TopLeft:
                AdmobBannerPos = AdPosition.TopLeft;
                break;
            case BannerPos.TopRight:
                AdmobBannerPos = AdPosition.TopRight;
                break;
            case BannerPos.Bottom:
                AdmobBannerPos = AdPosition.Bottom;
                break;
            case BannerPos.BottomLeft:
                AdmobBannerPos = AdPosition.BottomLeft;
                break;
            case BannerPos.BottomRight:
                AdmobBannerPos = AdPosition.BottomRight;
                break;
        }
    }

    public void SetBanner2Pos(BannerPos Pos)
    {
        switch (Pos)
        {
            case BannerPos.Top:
                AdmobBanner2Pos = AdPosition.Top;
                break;
            case BannerPos.TopLeft:
                AdmobBanner2Pos = AdPosition.TopLeft;
                break;
            case BannerPos.TopRight:
                AdmobBanner2Pos = AdPosition.TopRight;
                break;
            case BannerPos.Bottom:
                AdmobBanner2Pos = AdPosition.Bottom;
                break;
            case BannerPos.BottomLeft:
                AdmobBanner2Pos = AdPosition.BottomLeft;
                break;
            case BannerPos.BottomRight:
                AdmobBanner2Pos = AdPosition.BottomRight;
                break;
        }
    }

    #endregion

    #region HELPER METHODS

    private AdRequest CreateAdRequest()
    {
        return new AdRequest();
    }

    #endregion

    #region BANNER ADS

    public bool IsBannerAdAvailable
    {
        get
        {
            return (banner != null);
        }

    }

    public bool IsAdmobSmallBannerReady()
    {
        return isSmallBannerLoaded;
    }

    public void LoadAdmobSmallBanner()
    {
        if (!isRemoteAdsEnabled)
            return;
        if (!isAdmobInitialized || IsAdmobSmallBannerReady() || smallBannerStatus == AdLoadingStatus.Loading || adsStatus == AdType.NoAds)
        {
            Debug.Log("admobInt" + isAdmobInitialized);
            Debug.Log("BannerReady" + IsAdmobSmallBannerReady());
            Debug.Log("Loadingsts" + smallBannerStatus);
            Debug.Log("AdsSts" + adsStatus);

            Debug.Log("Admob_smallBanner No Request");
            //return;
        }
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork | Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            if (PlayerPrefs.GetInt("RemoveAds") != 1)
            {
                Debug.Log("Admob_smallBanner LoadRequest");
                smallBannerStatus = AdLoadingStatus.Loading;
                LoadAdmobBannerAd(ADMOB_ID.ADMOB_BANNER_AD_ID);
            }
        }
    }

    public void LoadAdmobBannerAd(string ID)
    {
        if (banner != null)
        {
            DestroyBannerAd();
        }

        banner = new BannerView(ID, bannerSize, AdmobBannerPos);

        banner.OnBannerAdLoaded += () =>
        {
            Debug.Log("Admob_SmallBanner_Loaded");
            smallBannerStatus = AdLoadingStatus.Loaded;
            isSmallBannerLoaded = true;
        };
        banner.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.Log("Admob_SmallBanner_NoInventory :: " + error.GetMessage());
            smallBannerStatus = AdLoadingStatus.NoInventory;
            isSmallBannerLoaded = false;
        };
        banner.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Admob_smallBanner_Displayed");
            Debug.Log("Admob_smallBanner recorded an impression");
        };
        banner.OnAdClicked += () =>
        {
            Debug.Log("Admob_smallBanner Click.");
        };
        banner.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Admob_smallBanner Opening.");
        };
        banner.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Admob:smallBanner Closed.");
        };
        banner.OnAdPaid += (AdValue adValue) =>
        {
            string msg = string.Format("{0} (currency: {1}, value: {2}",
                                        "Banner ad received a paid event.",
                                        adValue.CurrencyCode,
                                        adValue.Value);

            Debug.Log(msg);

        };

        // Load a banner ad
        AdRequest request = CreateAdRequest();
        banner.LoadAd(request);
        if (!isShowBanner)
        {
            banner.Hide();
        }
    }

    public void ShowAdmobSmallBanner(BannerPos position)
    {
        if (!isAdmobBannerInitialized || !isAdmobInitialized || adsStatus == AdType.NoAds)
        {
            return;
        }
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork | Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            if (PlayerPrefs.GetInt("RemoveAds") != 1)
            {
                SetBannerPos(position);
                Debug.Log("Admob_smallBanner ShowCall");
                if (IsBannerAdAvailable)
                {
                    Debug.Log("Admob_smallBanner Hide Previous");
                    banner.Hide();
                    Debug.Log("Admob_smallBanner WillDisplay");
                    banner.Show();
                    banner.SetPosition(AdmobBannerPos);
                }
                else
                {
                    Debug.Log("Admob_smallBanner No Ad Loaded");
                    LoadAdmobSmallBanner();
                }
            }
        }
    }

    public void ShowAdmobSmallBanner()
    {
        if (!isAdmobBannerInitialized || !isAdmobInitialized || adsStatus == AdType.NoAds)
        {
            return;
        }
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork | Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            if (PlayerPrefs.GetInt("RemoveAds") != 1)
            {
                Debug.Log("Admob_smallBanner ShowCall");
                if (IsBannerAdAvailable)
                {
                    Debug.Log("Admob_smallBanner Hide Previous");
                    banner.Hide();
                    Debug.Log("Admob_smallBanner WillDisplay");
                    banner.Show();
                    banner.SetPosition(AdmobBannerPos);
                }
                else
                {
                    Debug.Log("Admob_smallBanner No Ad Loaded");
                    LoadAdmobSmallBanner();
                }
            }
        }
    }

    public void HideAdmobSmallBanner()
    {
        if (banner != null)
        {
            Debug.Log("Admob_smallBanner_Hide");
            banner.Hide();
        }
    }

    public void DestroyBannerAd()
    {
        if (banner != null)
        {
            Debug.Log("Admob_smallBanner  Destroyed");
            banner.Destroy();
            banner = null;
        }
    }

    #endregion

    #region BANNER2 ADS

    public bool IsBanner2AdAvailable
    {
        get
        {
            return (banner2 != null);
        }

    }

    public bool IsAdmobSmallBanner2Ready()
    {
        return isSmallBanner2Loaded;
    }

    public void LoadAdmobSmallBanner2()
    {
        if (!isRemoteAdsEnabled)
            return;
        if (!isAdmobInitialized || IsAdmobSmallBanner2Ready() || smallBanner2Status == AdLoadingStatus.Loading || adsStatus == AdType.NoAds)
        {
            Debug.Log("Admob_smallBanner2 No Request Generated");
            return;
        }
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork | Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            if (PlayerPrefs.GetInt("RemoveAds") != 1)
            {
                Debug.Log("Admob_smallBanner2 LoadRequest");
                smallBanner2Status = AdLoadingStatus.Loading;
                LoadAdmobBanner2Ad(ADMOB_ID.ADMOB_BANNER_AD_ID_2);
            }
        }
    }

    public void LoadAdmobBanner2Ad(string ID)
    {
        // Clean up banner before reusing
        if (banner2 != null)
        {
            DestroyBanner2Ad();
        }

        // Create a 320x50 banner at top of the screen
        banner2 = new BannerView(ID, banner2Size, AdmobBanner2Pos);

        // Add Event Handlers
        banner2.OnBannerAdLoaded += () =>
        {
            Debug.Log("Admob_smallBanner2:Loaded.");
            smallBanner2Status = AdLoadingStatus.Loaded;
            isSmallBanner2Loaded = true;
        };
        banner2.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.Log("Admob_smallBanner2:NoInventory :: " + error.GetMessage());
            smallBanner2Status = AdLoadingStatus.NoInventory;
            isSmallBanner2Loaded = false;
        };
        banner2.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Admob_smallBanner2:Displayed");
            Debug.Log("Admob_smallBanner2:Displayed recorded an impression");
        };
        banner2.OnAdClicked += () =>
        {
            Debug.Log("Admob_smallBanner2 : Click.");
        };
        banner2.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Admob_smallBanner2 : Opening.");
        };
        banner2.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Admob_smallBanner2 : Closed.");
        };
        banner2.OnAdPaid += (AdValue adValue) =>
        {
            string msg = string.Format("{0} (currency: {1}, value: {2}",
                                        "Banner2 ad received a paid event.",
                                        adValue.CurrencyCode,
                                        adValue.Value);

            Debug.Log(msg);

        };

        // Load a banner ad
        AdRequest request = CreateAdRequest();
        banner2.LoadAd(request);
        if (!isShowBanner)
        {
            banner2.Hide();
        }
    }

    public void ShowAdmobSmallBanner2(BannerPos position)
    {
        if (!isAdmobBanner2Initialized || !isAdmobInitialized || adsStatus == AdType.NoAds)
        {
            return;
        }
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork | Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            if (PlayerPrefs.GetInt("RemoveAds") != 1)
            {
                SetBanner2Pos(position);
                Debug.Log("Admob_smallBanner2 ShowCall");
                if (IsBanner2AdAvailable)
                {
                    Debug.Log("Admob_smallBanner2 Hide Previous");
                    banner2.Hide();
                    Debug.Log("Admob_smallBanner2 WillDisplay");
                    banner2.Show();
                    banner2.SetPosition(AdmobBanner2Pos);
                }
                else
                {
                    Debug.Log("Admob_smallBanner2 No Ad Loaded");
                    LoadAdmobSmallBanner2();
                }
            }
        }
    }

    public void ShowAdmobSmallBanner2()
    {
        if (!isAdmobBanner2Initialized || !isAdmobInitialized || adsStatus == AdType.NoAds)
        {
            return;
        }
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork | Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            if (PlayerPrefs.GetInt("RemoveAds") != 1)
            {
                Debug.Log("Admob:smallBanner2:ShowCall");
                if (IsBanner2AdAvailable)
                {
                    Debug.Log("Admob:smallBanner2:Hide Previous");
                    banner2.Hide();
                    Debug.Log("Admob:smallBanner2:WillDisplay");
                    banner2.Show();
                    banner2.SetPosition(AdmobBanner2Pos);
                }
                else
                {
                    Debug.Log("Admob:smallBanner2: No Ad Loaded");
                    LoadAdmobSmallBanner2();
                }
            }
        }
    }

    public void HideAdmobSmallBanner2()
    {
        if (banner2 != null)
        {
            Debug.Log("Admob:smallBanner2:Hide");
            banner2.Hide();
        }
    }

    public void DestroyBanner2Ad()
    {
        if (banner2 != null)
        {
            Debug.Log("Admob:smallBanner2 : Destroyed");
            banner2.Destroy();
            banner2 = null;
        }
    }

    #endregion

    #region MEDIUM BANNER ADS

    public bool IsMediumBannerAdAvailable
    {
        get
        {
            return (mediumbanner != null);
        }
    }

    public bool IsMediumBannerReady()
    {
        return isMediumBannerLoaded;
    }

    public void LoadAdmobMediumBanner()
    {
        if (!isRemoteAdsEnabled)
            return;
        if (!isAdmobInitialized || IsMediumBannerReady() || mediumBannerStatus == AdLoadingStatus.Loading || adsStatus == AdType.NoAds)
        {
            return;
        }
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork | Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            if (PlayerPrefs.GetInt("RemoveAds") != 1)
            {
                Debug.Log("Admob:mediumBanner:LoadRequest");
                mediumBannerStatus = AdLoadingStatus.Loading;
                LoadMediumBannerAd(ADMOB_ID.ADMOB_MEDIUM_BANNER_AD_ID);
            }
        }
    }

    public void LoadMediumBannerAd(string ID)
    {
        // Clean up banner before reusing
        if (mediumbanner != null)
        {
            DestroyMediumBannerAd();
        }

        // Create a medium banner at the screen
        mediumbanner = new BannerView(ID, AdSize.MediumRectangle, _AdmobMediumBannerPos);

        // Add Event Handlers
        mediumbanner.OnBannerAdLoaded += () =>
        {
            Debug.Log("Admob:mediumBanner:Loaded.");
            mediumBannerStatus = AdLoadingStatus.Loaded;
            isMediumBannerLoaded = true;
        };
        mediumbanner.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.Log("Admob:mediumBanner:NoInventory :: " + error.GetMessage());
            mediumBannerStatus = AdLoadingStatus.NoInventory;
            isMediumBannerLoaded = false;
        };
        mediumbanner.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Admob:mediumBanner:Displayed");
            Debug.Log("Admob:mediumBanner:Displayed recorded an impression");
        };
        mediumbanner.OnAdClicked += () =>
        {
            Debug.Log("Admob:mediumBanner : Click.");
        };
        mediumbanner.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Admob:mediumBanner : Opening.");
        };
        mediumbanner.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Admob:mediumBanner : Closed.");
        };
        mediumbanner.OnAdPaid += (AdValue adValue) =>
        {
            string msg = string.Format("{0} (currency: {1}, value: {2}",
                                        "mediumBanner received a paid event.",
                                        adValue.CurrencyCode,
                                        adValue.Value);
            Debug.Log(msg);

        };

        // Load a banner ad
        AdRequest request = CreateAdRequest();
        mediumbanner.LoadAd(request);
        mediumbanner.Hide();
    }

    public void ConcealBottomBanner()
    {
        if (mediumbanner != null)
        {
            Debug.Log("Admob:mediumbanner:Hide");
            mediumbanner.Hide();
        }
    }

    public void DisplayBottomBanner()
    {
        try
        {
            if (!isAdmobBannerInitialized || !isAdmobInitialized || adsStatus == AdType.NoAds)
            {
                return;
            }

            if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork | Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            {
                if (PlayerPrefs.GetInt("RemoveAds") != 1)
                {
                    Debug.Log("Admob:MediumBanner:ShowCall");
                    if (IsMediumBannerAdAvailable)
                    {
                        Debug.Log("Admob:MediumBanner:WillDisplay");
                        mediumbanner.Show();
                        mediumbanner.SetPosition(_AdmobMediumBannerPos);
                    }
                    else
                    {
                        Debug.Log("Admob: MediumBanner: No Ad Loaded");
                        LoadAdmobMediumBanner();
                    }
                }
            }
        }
        catch (Exception error)
        {
            Debug.Log("Medium Banner Error: " + error);
        }

    }

    public void DestroyMediumBannerAd()
    {
        if (mediumbanner != null)
        {
            Debug.Log("Admob: Medium Banner Ad Destroyed.");
            mediumbanner.Destroy();
            mediumbanner = null;
        }
    }

    #endregion

    #region INTERSTITIAL ADS

    public bool IsInterstitialAdAvailable
    {
        get
        {
            return (interstitialAd != null
                && interstitialAd.CanShowAd());
        }
    }

    public bool IsInterstitialAdReady()
    {
        if (interstitialAd != null)
            return interstitialAd.CanShowAd();
        else
            return false;
    }

    public void LoadAdmobInterstitial()
    {
        if (!isAdmobInitialized || IsInterstitialAdReady() || iAdStatus == AdLoadingStatus.Loading || adsStatus == AdType.NoAds)
        {
            return;
        }
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork | Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            if (PlayerPrefs.GetInt("RemoveAds") != 1)
            {
                Debug.Log("Admob: inter ad :LoadRequest");
                iAdStatus = AdLoadingStatus.Loading;
                LoadInterAd(ADMOB_ID.ADMOB_INTERTITIAL_AD_ID);
            }

        }
    }

    public void LoadInterAd(string ID)
    {
        // Clean up interstitial before using it
        if (interstitialAd != null)
        {
            DestroyInterstitialAd();
        }

        // Load an interstitial ad
        InterstitialAd.Load(ID, CreateAdRequest(),
            (InterstitialAd ad, LoadAdError loadError) =>
            {
                if (loadError != null)
                {
                    iAdStatus = AdLoadingStatus.NoInventory;
                    Debug.Log("Admob:Interstitial Ad failed to load with error: " +
                        loadError.GetMessage());
                    return;
                }
                else if (ad == null)
                {
                    iAdStatus = AdLoadingStatus.NoInventory;
                    Debug.Log("Admob:Interstitial Ad failed to load.");
                    return;
                }

                iAdStatus = AdLoadingStatus.Loaded;
                Debug.Log("Admob:Interstitial Ad loaded.");
                interstitialAd = ad;

                ad.OnAdFullScreenContentOpened += () =>
                {
                    iAdStatus = AdLoadingStatus.NotLoaded;
                    Debug.Log("Admob:Interstitial Ad open.");
                };
                ad.OnAdFullScreenContentClosed += () =>
                {
                    iAdStatus = AdLoadingStatus.NotLoaded;
                    Debug.Log("Admob: Interstitial Ad closed.");
                    OppenApp_Not_Shown = true;
                    LoadAdmobInterstitial();
                    ShowAdmobAppOpenAd();
                };
                ad.OnAdImpressionRecorded += () =>
                {
                    Debug.Log("Admob:Interstitial Ad recorded an impression.");
                };
                ad.OnAdClicked += () =>
                {
                    Debug.Log("Admob:Interstitial Ad recorded a click.");
                    OppenApp_Not_Shown = true;
                };
                ad.OnAdFullScreenContentFailed += (AdError error) =>
                {
                    Debug.Log("Admob:Interstitial Ad failed to show with error: " +
                                error.GetMessage());
                };
                ad.OnAdPaid += (AdValue adValue) =>
                {
                    string msg = string.Format("{0} (currency: {1}, value: {2}",
                                               "Interstitial ad received a paid event.",
                                               adValue.CurrencyCode,
                                               adValue.Value);
                    Debug.Log(msg);

                };
            });
    }

    public void ShowAdmobInterstitial()
    {
        if (!isRemoteAdsEnabled)
            return;
        if (!isAdmobInitialized || adsStatus == AdType.NoAds)
        {
            return;
        }

        if (PlayerPrefs.GetInt("RemoveAds") != 1)
        {
            if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork || Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            {
                Debug.Log("Admob:iad:ShowCall");
                if (IsInterstitialAdAvailable)
                {
                    OppenApp_Not_Shown = true;
                    Debug.Log("Admob:iad:WillDisplay (With 2s Loading)");

                    StartCoroutine(ShowInterstitialWithDelay());
                }
                else
                {
                    Debug.Log("Admob:Interstitial Ad No Inventory.");
                    LoadAdmobInterstitial();
                }
            }
            else
            {
                Debug.Log("Admob:iad:ShowCall:Offline");
            }
        }
    }

    private IEnumerator ShowInterstitialWithDelay()
    {
        if (loadAdPanel != null) loadAdPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(2f);
        if (loadAdPanel != null) loadAdPanel.SetActive(false);

        interstitialAd.Show();
    }

    public void DestroyInterstitialAd()
    {
        if (interstitialAd != null)
        {
            Debug.Log("Admob:Interstitial Ad Destroyed.");
            interstitialAd.Destroy();
            interstitialAd = null;
        }
    }

    #endregion

    #region REWARDED ADS

    public bool IsRewardedAdAvailable
    {
        get
        {
            return (rewardedAd != null
                && rewardedAd.CanShowAd());
        }
    }

    public bool IsRewardedAdReady()
    {
        if (rewardedAd != null)
            return rewardedAd.CanShowAd();
        else
            return false;
    }

    public void LoadAdmobRewardedVideo()
    {
        if (!isRemoteAdsEnabled)
            return;

        if (!isAdmobInitialized || IsRewardedAdReady() || rAdStatus == AdLoadingStatus.Loading || adsStatus == AdType.NoAds)
        {
            return;
        }

        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork | Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            Debug.Log("Admob:rad:LoadRequest");
            LoadRewardedAd(ADMOB_ID.ADMOB_REWARDED_AD_ID);
            rAdStatus = AdLoadingStatus.Loading;
        }
    }

    public void LoadRewardedAd(string ID)
    {
        // Clean up Rewarded before using it
        if (rewardedAd != null)
        {
            DestroyRewardedlAd();
        }

        // create new rewarded ad instance
        RewardedAd.Load(ID, CreateAdRequest(),
            (RewardedAd ad, LoadAdError loadError) =>
            {
                if (loadError != null)
                {
                    rAdStatus = AdLoadingStatus.NoInventory;
                    Debug.Log("Admob: Rewarded Ad failed to load with error: " +
                                loadError.GetMessage());
                    return;
                }
                else if (ad == null)
                {
                    rAdStatus = AdLoadingStatus.NoInventory;
                    Debug.Log("Admob: Rewarded Ad failed to load.");
                    return;
                }

                Debug.Log("Admob: Rewarded Ad loaded.");
                rAdStatus = AdLoadingStatus.Loaded;
                rewardedAd = ad;

                ad.OnAdFullScreenContentOpened += () =>
                {
                    rAdStatus = AdLoadingStatus.NotLoaded;
                    Debug.Log("Admob: Rewarded Ad opening.");
                };
                ad.OnAdFullScreenContentClosed += () =>
                {
                    rAdStatus = AdLoadingStatus.NotLoaded;
                    OppenApp_Not_Shown = true;

                    Debug.Log("Admob: Rewarded Ad closed.");
                    LoadAdmobRewardedVideo();
                };
                ad.OnAdImpressionRecorded += () =>
                {
                    rAdStatus = AdLoadingStatus.NotLoaded;
                    Debug.Log("Admob: Rewarded Ad recorded an impression.");
                };
                ad.OnAdClicked += () =>
                {
                    Debug.Log("Admob: Rewarded Ad recorded a click.");
                    OppenApp_Not_Shown = true;

                };
                ad.OnAdFullScreenContentFailed += (AdError error) =>
                {
                    Debug.Log("Admob: Rewarded Ad failed to show with error: " +
                               error.GetMessage());

                };
                ad.OnAdPaid += (AdValue adValue) =>
                {
                    string msg = string.Format("{0} (currency: {1}, value: {2}",
                                               "Rewarded ad received a paid event.",
                                               adValue.CurrencyCode,
                                               adValue.Value);
                    Debug.Log(msg);
                };
            });
    }

    public void DisplayRewardedAd(RewardUserDelegate _delegate)
    {
        if (!isRemoteAdsEnabled)
        {
            EnableNoAdLoaded();
            return;
        }
        if (!isAdmobInitialized || adsStatus == AdType.NoAds)
        {
            return;
        }
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork | Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            Debug.Log("Admob:rad:ShowCall");
            NotifyReward = _delegate;
            if (IsRewardedAdAvailable)
            {
                Debug.Log("Admob: Rewarded Ad WillDisplay. (With 2s Loading)");
                OppenApp_Not_Shown = true;
                
                StartCoroutine(ShowRewardedWithDelay());
            }
            else
            {
                Debug.Log("Admob: Rewarded Ad No Inventory.");
                EnableNoAdLoaded();
                LoadAdmobRewardedVideo();
            }
        }
        else
        {
            EnableNoAdLoaded();
        }
    }

    private IEnumerator ShowRewardedWithDelay()
    {
        if (loadAdPanel != null) loadAdPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(2f);
        if (loadAdPanel != null) loadAdPanel.SetActive(false);

        rewardedAd.Show((GoogleMobileAds.Api.Reward reward) =>
        {
            //PrintStatus(" Rewarded Ad granted a reward: " + reward.Amount);
            Debug.Log("give reward to user after watching Rewarded Ad");
            NotifyReward();
        });
    }

    public void DestroyRewardedlAd()
    {
        if (rewardedAd != null)
        {
            Debug.Log("Admob: Rewarded Ad Destroyed.");
            rewardedAd.Destroy();
            rewardedAd = null;
        }
    }

    #endregion

    #region APPOPEN ADS

    public bool IsAppOpenAdAvailable
    {
        get
        {
            return (appOpenAd != null
                    && appOpenAd.CanShowAd()
                    && DateTime.Now < appOpenExpireTime);
        }

    }

    public void LoadAdmobAppOpenAd()
    {
   
        if (!isAdmobInitialized || IsAppOpenAdAvailable || adsStatus == AdType.NoAds)
        {
            return;
        }
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork | Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            if (PlayerPrefs.GetInt("RemoveAds") != 1)
            {
                Debug.Log("Admob: AppOpen Ad : LoadRequest");
                LoadAdmobOpenAd(ADMOB_ID.ADMOB_AppOpen_AD_ID);
            }
        }
    }


    public void LoadAdmobOpenAd(string ID)
    {
        // destroy old instance.
        if (appOpenAd != null)
        {
            DestroyAppOpenAd();
        }

        // Create a new app open ad instance.
        AppOpenAd.Load(ID, CreateAdRequest(),
            (AppOpenAd ad, LoadAdError loadError) =>
            {
                if (loadError != null)
                {
                    Debug.Log("Admob: AppOpen Ad : failed to load with error: " +
                        loadError.GetMessage());
                    return;
                }
                else if (ad == null)
                {
                    Debug.Log("Admob: AppOpen Ad : failed to load : ");
                    return;
                }

                Debug.Log("Admob: AppOpen Ad : loaded. Please background the app and return.");
                this.appOpenAd = ad;
                this.appOpenExpireTime = DateTime.Now + APPOPEN_TIMEOUT;

                ad.OnAdFullScreenContentOpened += () =>
                {
                    Debug.Log("Admob: AppOpen Ad : opened : ");
                };
                ad.OnAdFullScreenContentClosed += () =>
                {
                    LoadAdmobAppOpenAd();
                    ShowAdmobAppOpenAd2();
                };
                ad.OnAdImpressionRecorded += () =>
                {
                    Debug.Log("Admob: App open ad recorded an impression : ");
                };
                ad.OnAdClicked += () =>
                {
                    Debug.Log("Admob: App open ad recorded a click : ");
                };
                ad.OnAdFullScreenContentFailed += (AdError error) =>
                {
                    Debug.Log("Admob: App open ad failed to show with error: " +
                        error.GetMessage());
                };
                ad.OnAdPaid += (AdValue adValue) =>
                {
                    string msg = string.Format("{0} (currency: {1}, value: {2}",
                                               "App open ad received a paid event.",
                                               adValue.CurrencyCode,
                                               adValue.Value);

                    Debug.Log(msg);


                };

                if (firstOpen)
                {
                    firstOpen = false;
                    Debug.Log("FirstOpen AppOpen Ad");
                    //ShowAdmobAppOpenAd();    //..............For Application Focus Disable this line....................
                }
            });
    }

    public void DestroyAppOpenAd()
    {
        if (this.appOpenAd != null)
        {
            this.appOpenAd.Destroy();
            this.appOpenAd = null;
        }
    }

    public void ShowAdmobAppOpenAd()
    {
      
        if (!isAdmobInitialized || adsStatus == AdType.NoAds)
        {
            return;
        }
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork | Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            if (PlayerPrefs.GetInt("RemoveAds") != 1)
            {
                Debug.Log("Admob: AppOpen Ad:ShowCall");
                if (IsAppOpenAdAvailable)
                {
                    Debug.Log("Admob: AppOpen Ad:WillDisplay");
                    appOpenAd.Show();
                }
                else
                {
                    Debug.Log("Admob: AppOpen Ad: No Ad Loaded");
                    LoadAdmobAppOpenAd();
                }
            }
        }
    }

    #endregion

    #region APPOPEN2 ADS

    public bool IsAppOpenAd2Available
    {
        get
        {
            return (appOpenAd2 != null
                    && appOpenAd2.CanShowAd()
                    && DateTime.Now < appOpenExpireTime2);
        }
    }

    public void LoadAdmobAppOpenAd2()
    {
        if (!isAdmobInitialized || IsAppOpenAd2Available || adsStatus == AdType.NoAds)
        {
            return;
        }

        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork ||
            Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            if (PlayerPrefs.GetInt("RemoveAds") != 1)
            {
                Debug.Log("Admob: AppOpen2 Ad : LoadRequest");
                LoadAdmobOpenAd2(ADMOB_ID.ADMOB_AppOpen_AD_ID_2);
            }
        }
    }
    public void LoadAdmobOpenAd2(string ID)
    {
        if (appOpenAd2 != null)
        {
            DestroyAppOpenAd2();
        }

        AppOpenAd.Load(ID, CreateAdRequest(),
            (AppOpenAd ad, LoadAdError loadError) =>
            {
                if (loadError != null)
                {
                    Debug.Log("Admob: AppOpen2 Ad : failed to load with error: " + loadError.GetMessage());
                    return;
                }
                else if (ad == null)
                {
                    Debug.Log("Admob: AppOpen2 Ad : failed to load");
                    return;
                }

                Debug.Log("Admob: AppOpen2 Ad : loaded");
                this.appOpenAd2 = ad;
                this.appOpenExpireTime2 = DateTime.Now + APPOPEN_TIMEOUT2;

                ad.OnAdFullScreenContentOpened += () =>
                {
                    Debug.Log("Admob: AppOpen2 Ad : opened");
                };

                ad.OnAdFullScreenContentClosed += () =>
                {
                    Debug.Log("Admob: AppOpen2 Ad : closed");
                    LoadAdmobAppOpenAd2();
                };

                ad.OnAdImpressionRecorded += () =>
                {
                    Debug.Log("Admob: AppOpen2 Ad : impression recorded");
                };

                ad.OnAdClicked += () =>
                {
                    Debug.Log("Admob: AppOpen2 Ad : clicked");
                };

                ad.OnAdFullScreenContentFailed += (AdError error) =>
                {
                    Debug.Log("Admob: AppOpen2 Ad failed to show with error: " + error.GetMessage());
                };

                ad.OnAdPaid += (AdValue adValue) =>
                {
                    string msg = string.Format("{0} (currency: {1}, value: {2}",
                                               "AppOpen2 ad received a paid event.",
                                               adValue.CurrencyCode,
                                               adValue.Value);
                    Debug.Log(msg);
                };

                if (firstOpen2)
                {
                    firstOpen2 = false;
                    Debug.Log("FirstOpen AppOpen2 Ad");
                    //ShowAdmobAppOpenAd2();
                }
            });
    }
    public void ShowAdmobAppOpenAd2()
    {

        if (!isAdmobInitialized || adsStatus == AdType.NoAds)
        {
            return;
        }

        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork ||
            Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            if (PlayerPrefs.GetInt("RemoveAds") != 1)
            {
                Debug.Log("Admob: AppOpen2 Ad:ShowCall");
                if (IsAppOpenAd2Available)
                {
                    Debug.Log("Admob: AppOpen2 Ad:WillDisplay");
                    appOpenAd2.Show();
                }
                else
                {
                    Debug.Log("Admob: AppOpen2 Ad: No Ad Loaded");
                    LoadAdmobAppOpenAd2();
                }
            }
        }
    }

    public void DestroyAppOpenAd2()
    {
        if (this.appOpenAd2 != null)
        {
            this.appOpenAd2.Destroy();
            this.appOpenAd2 = null;
        }
    }

    #endregion

    #endregion

    #region Custom Ads Calling

    public void RemoveAllAds()
    {
        HideAdmobSmallBanner();
        HideAdmobSmallBanner2();
        ConcealBottomBanner();
    }

    public void HideSmallBannerGameplay()
    {
        HideAdmobSmallBanner2();
    }

    public void ShowMediumBanner()
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            DisplayBottomBanner();
        }
    }

    public void HideMediumBanner()
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            ConcealBottomBanner();
        }
    }

    public void ShowSmallBanner()
    {
        // Show Banner Ads.............................
        Debug.Log("ShowBanner");
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            Debug.Log("CallBanner");
            ShowAdmobSmallBanner();
            ShowAdmobSmallBanner2();
        }
    }

    public void ShowInterstitialAd()
    {
        Debug.Log("ShowInterAd");
        if (isRemoteAdsEnabled)
            ShowAdmobInterstitial();
    }

    #region Ad Break Logic
    public void StartAdBreakRoutine()
    {
        if (adBreakRoutine != null)
            StopCoroutine(adBreakRoutine);
        adBreakRoutine = StartCoroutine(AdBreakCoroutine());
    }

    public void StopAdBreakRoutine()
    {
        if (adBreakRoutine != null)
        {
            StopCoroutine(adBreakRoutine);
            adBreakRoutine = null;
        }
        if (adBreakPanel != null) adBreakPanel.SetActive(false);
    }

    private IEnumerator AdBreakCoroutine()
    {
        while (GameManager.Instance.isGameStarted)
        {
            // Wait for 30 seconds
            yield return new WaitForSeconds(adBreakInterval);

            // Show Panel
            if (adBreakPanel != null) adBreakPanel.SetActive(true);

            // Countdown for 10 seconds
            float timer = adBreakCountdownTime;
            while (timer > 0)
            {
                if (adBreakCountdownTxt != null)
                    adBreakCountdownTxt.text = "Ad Break in " + Mathf.CeilToInt(timer);

                timer -= Time.deltaTime;
                yield return null;
            }

            // Show Interstitial
            ShowInterstitialAd();

            // Hide Panel
            if (adBreakPanel != null) adBreakPanel.SetActive(false);
        }
    }
    #endregion

    public void DisplayInterstitialAd()
    {
        //if (Application.internetReachability != NetworkReachability.NotReachable)
        //{
        //    ShowAdmobInterstitial();
        //}
        Debug.Log("ShowInterAd");
        ShowAdmobInterstitial();

    }

    [ContextMenu("RemoveAds")]
    public void RemoveAds()
    {
        if (PlayerPrefs.GetInt("RemoveAds") != 1)
            PlayerPrefs.SetInt("RemoveAds", 1);
        RemoveAllAds();
    }
    #endregion

}