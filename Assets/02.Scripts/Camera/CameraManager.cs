﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum ECameraState
{
    DEFAULT = 0,
    ZOOMIN = 1,
    ZOOMOUT,
    STAY
}
public class CameraManager : MonoBehaviour
{
    [Tooltip("사용할 카메라. 넣지 않을 경우 자동으로 메인 카메라를 넣습니다. ")]
    public Camera currentCamera;

    [Tooltip("팔로우, 줌인, 줌아웃 대상")]
    public Transform target;

    [Tooltip("size에 따라서 스케일을 변화시킬 오브젝트...")]
    public GameObject BGObject;

    //위 오브젝트의 스케일 x,y값.
    private float scObjX = 0f;
    private float scObjY = 0f;

    private Vector3 scObjVel = Vector3.zero;

    [Header("기본 설정")]
    [Tooltip("카메라의 기본 z값 입니다. 줌과는 관련 없습니다.")]
    public float cameraDefaultPositionZ = -10f;

    [Tooltip("카메라의 원래...그...원래 사이즈. 원래 줌?")]
    public float cameraDefaultSize = 5f;

    [Tooltip("카메라 줌 인의 최대치")]
    public float cameraZoomInSize = 3f;

    [Tooltip("카메라 줌 아웃의 최대치")]
    public float cameraZoomOutSize = 10f;

    [Tooltip("팔로우 속도")]
    public float followSpeed;

    [Tooltip("줌인/줌아웃 속도. 시간모드일 경우 속도가 아닌 '초'로 작용함.")]
    public float zoomSpeed;

    [Tooltip("시간모드인가? : 줌인/줌아웃 시, 속도는 신경쓰지 않고 무조건 zoomSpeed초 안에 줌인/줌아웃을 끝냄.")]
    public bool isTimeMode;


    private float velocity = 0.0f;//줌 가속도

    public float zoomTimer = 0f;
    public float currentZoomSpeed;

    [SerializeField]
    private ECameraState cameraState;

    [Header("카메라 제한영역 설정")]
    [Tooltip("제한 영역 설정을 할 것인가?")]
    public bool isConfine;

    public Vector2 confinePos;
    public Vector2 confineSize;

    private Vector3 currentConfinePos;
    private Vector3 currentChangeSize;
    private float height;
    private float width;

    private IEnumerator currentCoroutine;

    private float limitCalSize;

    private Vector3 originBGObjectScale;

    public static CameraManager instance;
    public void Init()
    {
        if (instance == null)
            instance = this;


        height = currentCamera.orthographicSize;
        width = height * Screen.width / Screen.height;

        #region if <=0

        if (currentCamera == null)
        {
            Debug.Log("currentCamera가 null");
        }
        if (zoomSpeed <= 0f)
        {
            zoomSpeed = 1f;
        }

        if (followSpeed <= 0f)
        {
            followSpeed = 5f;
        }

        if (BGObject == null)
        {
            BGObject = new GameObject();
            //그냥 오류 방지를 위해 빈깡 하나 넣기.
        }
        #endregion

        limitCalSize = 0.03f;
        currentZoomSpeed = 1f / zoomSpeed;
        cameraState = ECameraState.DEFAULT;
        isTimeMode = true;

        scObjX = BGObject.transform.localScale.x;
        scObjY = BGObject.transform.localScale.y;
        //cameraDefaultPositionZ = -10f;

        originBGObjectScale = BGObject.transform.localScale;
    }

    private void Awake()
    {
        Init();
    }


    private void Start()
    {
        StartCoroutine(CameraControl());
    }

    private void Update()
    {
        //zoomSpeed가 변할...수도 있기 때문에.
        currentZoomSpeed = 1f / zoomSpeed;

        if (InputManager.Instance.buttonScroll.ReadValue().y > 0)
        {

            cameraState = ECameraState.ZOOMIN;
        }

        if (InputManager.Instance.buttonScroll.ReadValue().y < 0)
        {

            cameraState = ECameraState.ZOOMOUT;
        }



    }

    private void FixedUpdate()
    {
        FollowTarget(); // 타겟 팔로잉
    }

    private void FollowTarget() // 타겟 팔로잉
    {
        currentCamera.transform.position = Vector3.Lerp(currentCamera.transform.position, target.position, Time.smoothDeltaTime * followSpeed);


        if (isConfine) //제한 설정이 되어있으면 
        {
            currentConfinePos = GetConfinePosition();
            currentCamera.transform.position = new Vector3(currentConfinePos.x, currentConfinePos.y, cameraDefaultPositionZ);
        }
        else
        {
            currentCamera.transform.position = new Vector3(currentCamera.transform.position.x, currentCamera.transform.position.y, cameraDefaultPositionZ);
        }

    }

    private IEnumerator CameraControl()
    {
        while (true)
        {
            switch (cameraState)
            {
                case ECameraState.DEFAULT:
                    if (currentCamera.orthographicSize > cameraDefaultSize)
                    {
                        currentCoroutine = CameraZoomIn(cameraDefaultSize);
                    }
                    else if (currentCamera.orthographicSize < cameraDefaultSize)
                    {
                        currentCoroutine = CameraZoomOut(cameraDefaultSize);
                    }
                    else
                    {
                        currentCoroutine = null;
                    }
                    break;

                case ECameraState.ZOOMIN:
                    currentCoroutine = CameraZoomIn(cameraZoomInSize);
                    break;

                case ECameraState.ZOOMOUT:
                    currentCoroutine = CameraZoomOut(cameraZoomOutSize);
                    break;

                case ECameraState.STAY:
                    currentCoroutine = null;
                    break;

                default:
                    currentCoroutine = null;
                    break;
            }

            if (currentCoroutine != null)
            {
                yield return StartCoroutine(currentCoroutine);
            }
            else
            {

                yield return null;
            }
        }
    }
    private IEnumerator CameraZoomOut(float _size)
    {
        if (NowCameraState() == ECameraState.ZOOMIN)
        {
            _size = cameraDefaultSize;
        }

        if (isTimeMode) //시간 모드일 경우
        {
            zoomTimer = 0f;
            float oldOrthographicSize = currentCamera.orthographicSize; // 원래 사이즈를 저장

            while (Mathf.Abs(currentCamera.orthographicSize - _size) > limitCalSize)
            {
                zoomTimer += Time.smoothDeltaTime * currentZoomSpeed;
                currentCamera.orthographicSize = Mathf.Lerp(oldOrthographicSize, _size, zoomTimer);
                //스케일 변경 하고싶은 오브젝트의 스케일을...뭐 그 퍼센트만큼으로 변경시킨다. 되려나...

                // BG 크기 체인지
                ChangeScaleBGObject();

                yield return YieldInstructionCache.WaitForEndOfFrame;
            }
        }
        else //아닐 경우
        {

            while (Mathf.Abs(currentCamera.orthographicSize - _size) > limitCalSize)
            {
                //zoomTimer += Time.deltaTime;
                currentCamera.orthographicSize = Mathf.SmoothDamp(currentCamera.orthographicSize, _size, ref velocity, zoomSpeed);
                //ChangeScaleThisObject();

                // BG 크기 체인지
                ChangeScaleBGObject();

                yield return YieldInstructionCache.WaitForEndOfFrame;
            }
        }
        currentCamera.orthographicSize = _size;
        cameraState = ECameraState.STAY;



    }
    private IEnumerator CameraZoomIn(float _size)
    {
        if (NowCameraState() == ECameraState.ZOOMOUT)
        {
            _size = cameraDefaultSize;
        }

        if (isTimeMode) //시간 모드일 경우
        {
            zoomTimer = 0f;
            float oldOrthographicSize = currentCamera.orthographicSize; // 원래 사이즈를 저장

            while (Mathf.Abs(currentCamera.orthographicSize - _size) > limitCalSize)
            {
                zoomTimer += Time.smoothDeltaTime * currentZoomSpeed;
                currentCamera.orthographicSize = Mathf.Lerp(oldOrthographicSize, _size, zoomTimer);

                // BG 크기 체인지
                ChangeScaleBGObject();

                yield return YieldInstructionCache.WaitForEndOfFrame;
            }
        }
        else //아닐 경우
        {
            while (Mathf.Abs(currentCamera.orthographicSize - _size) > limitCalSize)
            {
                // zoomTimer += Time.deltaTime;
                currentCamera.orthographicSize = Mathf.SmoothDamp(currentCamera.orthographicSize, _size, ref velocity, zoomSpeed);

                // BG 크기 체인지
                ChangeScaleBGObject();

                yield return YieldInstructionCache.WaitForEndOfFrame;
            }
        }

        currentCamera.orthographicSize = _size;
        cameraState = ECameraState.STAY;

        // BG 크기 체인지
        ChangeScaleBGObject();
    }


    private Vector3 limitTest;
    private Vector3 GetConfinePosition()
    {
        height = currentCamera.orthographicSize;
        width = height * Screen.width / Screen.height;
        //애들 관련 그거를 하기 위해서는 이 부분에 아이들의 위치가 들어가야함.
        float localX = confineSize.x * 0.5f - width;

        float localY = confineSize.y * 0.5f - height;

        float clampX = Mathf.Clamp(currentCamera.transform.position.x, -localX + confinePos.x, localX + confinePos.x);
        float clampY = Mathf.Clamp(currentCamera.transform.position.y, -localY + confinePos.y, localY + confinePos.y);

        Vector3 centerBottom = Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0f));
        //가운데 아래의 좌표를 얻어야함....

        //float clampX = Mathf.Clamp(centerBottom.x, -localX + confinePos.x, localX + confinePos.x);
        //float clampY = Mathf.Clamp(centerBottom.y, -localY + confinePos.y, localY + confinePos.y);


        Vector3 confinePosition = new Vector3(clampX, clampY, cameraDefaultPositionZ);
        limitTest = new Vector3(clampX, clampY, cameraDefaultPositionZ);
        return confinePosition;
    }


    private ECameraState NowCameraState()
    {
        if (currentCamera.orthographicSize == cameraZoomOutSize)
        {
            return ECameraState.ZOOMOUT;
        }
        else if (currentCamera.orthographicSize == cameraZoomInSize)
        {
            return ECameraState.ZOOMIN;
        }
        else if (currentCamera.orthographicSize == cameraDefaultSize)
        {
            return ECameraState.DEFAULT;
        }
        else
        {
            return ECameraState.STAY;
        }


    }

    private void ChangeScaleBGObject()
    {
        float BgScaleRatio = (currentCamera.orthographicSize / cameraDefaultSize);
        Vector3 scaleValue = originBGObjectScale * BgScaleRatio;
        BGObject.transform.localScale = new Vector3(scaleValue.x, scaleValue.y, originBGObjectScale.z);
    }

    private void OnDrawGizmos()
    {
        if (isConfine)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(confinePos, confineSize);

            //Gizmos.color = Color.cyan;
            //Gizmos.DrawSphere(limitTest, 3f); 
            
            //Gizmos.color = Color.blue;
            //Gizmos.DrawSphere(currentConfinePos, 3f);
            
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawSphere(currentConfinePos, 3f);
        }
    }

}
