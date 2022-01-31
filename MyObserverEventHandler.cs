using System;
using UnityEngine;
using UnityEngine.Events;
using Vuforia;
using System.Collections;

public class MyObserverEventHandler : MonoBehaviour
{
    
    private string AssetName= "YourAssetName";
    private GameObject mBundleInstance = null;
    private AssetBundle bundle;

    private bool mAttached = false;

    public enum TrackingStatusFilter
    {
        Tracked,
        Tracked_ExtendedTracked,
        Tracked_ExtendedTracked_Limited
    }

    
    public TrackingStatusFilter StatusFilter = TrackingStatusFilter.Tracked_ExtendedTracked_Limited;
    public UnityEvent OnTargetFound;
    public UnityEvent OnTargetLost;


    protected ObserverBehaviour mObserverBehaviour;
    protected TargetStatus mPreviousTargetStatus = TargetStatus.NotObserved;
    protected bool mCallbackReceivedOnce;

    protected virtual void Start()
    {
	    StartCoroutine(DownloadAndCache());

        mObserverBehaviour = GetComponent<ObserverBehaviour>();

        if (mObserverBehaviour)
        {
            mObserverBehaviour.OnTargetStatusChanged += OnObserverStatusChanged;
            mObserverBehaviour.OnBehaviourDestroyed += OnObserverDestroyed;
            
            OnObserverStatusChanged(mObserverBehaviour, mObserverBehaviour.TargetStatus);
        }
    }
	
     
    IEnumerator DownloadAndCache()
    {
        while (!Caching.ready)
            yield return null;
        
            string bundleURL = "YourAssetUrl";
            
           
            using (WWW www = WWW.LoadFromCacheOrDownload(bundleURL, 1))
            {
                yield return www;
                if (www.error != null)
                    throw new UnityException("WWW Download had an error: " + www.error);
                bundle = www.assetBundle;
            }
        
    }

    protected virtual void OnDestroy()
    {
        if (mObserverBehaviour)
            OnObserverDestroyed(mObserverBehaviour);
    }
    
    void OnObserverDestroyed(ObserverBehaviour observer)
    {
        mObserverBehaviour.OnTargetStatusChanged -= OnObserverStatusChanged;
        mObserverBehaviour.OnBehaviourDestroyed -= OnObserverDestroyed;
        mObserverBehaviour = null;
    }
    
    void OnObserverStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        var name = mObserverBehaviour.TargetName;
        if (mObserverBehaviour is VuMarkBehaviour vuMarkBehaviour && vuMarkBehaviour.InstanceId != null)
        {
            name += " (" + vuMarkBehaviour.InstanceId + ")";
        }
        
        Debug.Log($"Target status: { name } { targetStatus.Status } -- { targetStatus.StatusInfo }");

        HandleTargetStatusChanged(mPreviousTargetStatus.Status, targetStatus.Status);
        HandleTargetStatusInfoChanged(targetStatus.StatusInfo);

        mPreviousTargetStatus = targetStatus;
    }

    protected virtual void HandleTargetStatusChanged(Status previousStatus, Status newStatus)
    {
        var shouldBeRendererBefore = ShouldBeRendered(previousStatus);
        var shouldBeRendererNow = ShouldBeRendered(newStatus);
        if (shouldBeRendererBefore != shouldBeRendererNow)
        {
            if (shouldBeRendererNow)
            {
		        mBundleInstance = Instantiate(bundle.LoadAsset(AssetName)) as GameObject;

            	mBundleInstance.transform.SetParent(mObserverBehaviour.gameObject.transform);
				
            	mBundleInstance.transform.localPosition = new Vector3(0f, 0f, 0f);
            	mBundleInstance.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            	mBundleInstance.transform.localScale = new Vector3(0f, 0f, 0f);
				
            	mBundleInstance.transform.gameObject.SetActive(true);
				
            	mAttached = true;

                OnTrackingFound();
            }
            else
            {
                OnTrackingLost();
		        Destroy(mBundleInstance);
            }
        }
        else
        {
            if (!mCallbackReceivedOnce && !shouldBeRendererNow)
            {
                // This is the first time we are receiving this callback, and the target is not visible yet.
                // --> Hide the augmentation.
                OnTrackingLost();
            }
        }

        mCallbackReceivedOnce = true;
    }

    protected virtual void HandleTargetStatusInfoChanged(StatusInfo newStatusInfo)
    {
        if (newStatusInfo == StatusInfo.WRONG_SCALE)
        {
            Debug.LogErrorFormat("The target {0} appears to be scaled incorrectly. " +
                                 "This might result in tracking issues. " +
                                 "Please make sure that the target size corresponds to the size of the " +
                                 "physical object in meters and regenerate the target or set the correct " +
                                 "size in the target's inspector.", mObserverBehaviour.TargetName);
        }
    }

    protected bool ShouldBeRendered(Status status)
    {
        if (status == Status.TRACKED)
        {
            // always render the augmentation when status is TRACKED, regardless of filter
            return true;
        }

        if (StatusFilter == TrackingStatusFilter.Tracked_ExtendedTracked && status == Status.EXTENDED_TRACKED)
        {
            // also return true if the target is extended tracked
            return true;
        }

        if (StatusFilter == TrackingStatusFilter.Tracked_ExtendedTracked_Limited && 
            (status == Status.EXTENDED_TRACKED || status == Status.LIMITED))
        {
            // in this mode, render the augmentation even if the target's tracking status is LIMITED.
            // this is mainly recommended for Anchors.
            return true;
        }

        return false;
    }

    protected virtual void OnTrackingFound()
    {
        if (mObserverBehaviour)
        {
            var rendererComponents = mObserverBehaviour.GetComponentsInChildren<Renderer>(true);
            var colliderComponents = mObserverBehaviour.GetComponentsInChildren<Collider>(true);
            var canvasComponents = mObserverBehaviour.GetComponentsInChildren<Canvas>(true);

            // Enable rendering:
            foreach (var component in rendererComponents)
                component.enabled = true;

            // Enable colliders:
            foreach (var component in colliderComponents)
                component.enabled = true;

            // Enable canvas':
            foreach (var component in canvasComponents)
                component.enabled = true;
        }

        OnTargetFound?.Invoke();
    }

    protected virtual void OnTrackingLost()
    {
        if (mObserverBehaviour)
        {
            var rendererComponents = mObserverBehaviour.GetComponentsInChildren<Renderer>(true);
            var colliderComponents = mObserverBehaviour.GetComponentsInChildren<Collider>(true);
            var canvasComponents = mObserverBehaviour.GetComponentsInChildren<Canvas>(true);

            // Disable rendering:
            foreach (var component in rendererComponents)
                component.enabled = false;

            // Disable colliders:
            foreach (var component in colliderComponents)
                component.enabled = false;

            // Disable canvas':
            foreach (var component in canvasComponents)
                component.enabled = false;
        }

        OnTargetLost?.Invoke();
    }
}