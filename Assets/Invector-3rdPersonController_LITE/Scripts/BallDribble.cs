using UnityEngine;
using UnityEngine.UI;
using Invector.vCharacterController;  // 이 부분 추가
using System.Collections;


public class BallDribble : MonoBehaviour
{
    [Header("References")]
    public Transform player;           
    public Transform rightHand;        
    public Rigidbody ballRigidbody;    

    [Header("Ball Interaction")]
    public float pickupRange = 2f;     // 공을 주울 수 있는 거리
    public KeyCode pickupKey = KeyCode.F;  // 공 줍기 키
    private bool hasBall = false;      // 공 소유 상태

    [Header("Base Dribble Settings")]
    public float catchHeight = 1f;         
    public float maxDribbleHeight = 1.3f;  
    public float maxDistance = 0.5f;       
    
    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.1f; 

    [Header("Movement Settings")]
    public float walkDribbleForce = 5f;      
    public float runDribbleForce = 8f;       
    public float walkBounceInterval = 0.25f; 
    public float runBounceInterval = 0.15f;  
    public float runMaxDistance = 0.5f;        
    
    [Header("Limits")]
    public float maxVelocity = 10f;

    [Header("Hand Hold Settings")]
    public float holdBallInHandDuration = 0.3f;

    [Header("Shoot Settings")]
    public Transform hoopTransform;     
    public float minShootForce = 5f;
    public float maxShootForce = 15f;
    public float maxChargeTime = 1.5f;
    public float shootArcHeight = 2f;
    private float shootChargeStartTime;  
    private bool isChargingShot = false;

    // 내부 변수들
    private CharacterController playerController;
    private Vector3 lastPlayerPosition;
    private float playerSpeed;
    private float upwardForce;    
    private float bounceInterval;
    private float nextBounceTime;

    private bool isDribbling = false;  // 시작할 때는 드리블 안하는 상태
    private bool isMovingToHand = false;
    private bool shouldReleaseShot = false;

    private ScoreManager scoreManager;
    private bool canScore = true; 
    private Animator animator;
    private float holdTimer = 0f;

    [Header("UI")]
    public Slider powerSlider;


    [Header("Shoot Trajectory")]
public float maxHeightOffset = 0.6f;  // private를 public으로 변경
public float range; 

    private vThirdPersonInput playerInput;


    [Header("Walking Dribble Parameters")]
    public float walkCatchHeight = 0.8f;
    public float walkMaxDribbleHeight = 1.1f;
    public float walkForwardOffset = 0.5f;
    public float walkSideOffset = 0.3f;
    public float walkBallStickiness = 5f;

    [Header("Running Dribble Parameters")]
    public float runCatchHeight = 1.2f;
    public float runMaxDribbleHeight = 1.5f;
    public float runForwardOffset = 0.8f;
    public float runSideOffset = 0.5f;
    public float runBallStickiness = 8f;

    [Header("Audio Settings")]
public AudioSource dribbleAudioSource;
public AudioClip dribbleSound;
public float minVolume = 0.4f;
public float maxVolume = 1.0f;
private bool canPlaySound = true;  // 새로운 변수 추가

    // 클래스 상단에 추가
private float currentForwardOffset;
private float currentSideOffset;
private float currentBallStickiness;
private float currentCatchHeight;
private float currentMaxDribbleHeight;

    [Header("Sprint Settings")]
    public KeyCode sprintInput = KeyCode.LeftShift;
    private bool isSprinting = false;

    [Header("Dunk Settings")]
    public float dunkRange = 3f;  // 덩크가 가능한 거리
    private bool isPerformingDunk = false;






     void Update()
    {
        HandleBallPickup();

        if(hasBall)
        {
            HandleShootInput();
            UpdatePowerUI();
            
            if(isChargingShot)
            {
                animator.SetBool("IsFacingAway", IsFacingAwayFromHoop());
            }
        }

        if(hasBall && isDribbling)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            
            float inputMagnitude = new Vector3(h, 0, v).magnitude;
            if(Input.GetKey(sprintInput))
            {
                isSprinting = true;
                inputMagnitude *= 1.5f;
            }
            else
            {
                isSprinting = false;
            }

            if(hasBall && isDribbling && !isChargingShot)
            {
                CheckDunkCondition();
            }
            
            animator.SetFloat("InputMagnitude", inputMagnitude);
        }
    }
    // 공 줍기 처리
    void HandleBallPickup()
    {
        if (!hasBall && Input.GetKeyDown(pickupKey))
        {
            float distanceToBall = Vector3.Distance(player.position, transform.position);
            
            if (distanceToBall <= pickupRange)
            {
                PickupBall();
            }
        }
    }

    void PickupBall()
    {
        hasBall = true;
        isDribbling = true;
        
        animator.SetBool("HasBall", true);
        animator.SetBool("IsDribbling", true);
        
        ballRigidbody.isKinematic = false;
        ballRigidbody.useGravity = true;
        transform.SetParent(null);
    }
     void UpdatePowerUI()
    {
        if(powerSlider != null)
        {
            if(isChargingShot)
            {
                powerSlider.gameObject.SetActive(true);
                float chargeTime = Mathf.Min(Time.time - shootChargeStartTime, maxChargeTime);
                powerSlider.value = chargeTime / maxChargeTime;
            }
            else
            {
                powerSlider.gameObject.SetActive(false);
                powerSlider.value = 0;
            }
        }
    }


   private float CalcMaxHeight(Vector3 startPos, Vector3 targetPos)
{
    // 지면상의 두 점 사이 거리 계산
    Vector3 direction = new Vector3(targetPos.x, 0f, targetPos.z) - 
                      new Vector3(startPos.x, 0f, startPos.z);
    float distance = direction.magnitude;  // 지역 변수로 변경
    
    // 공통 높이 (골대 높이 + 보정값)
    float maxYPos = targetPos.y + maxHeightOffset;
    
    // 45도 각도 유지를 위한 높이 조정
    if (distance / 2f > maxYPos)
        maxYPos = distance / 2f;
            
    return maxYPos;
}

    private Vector3 CalculateShootVelocity(Vector3 startPos, Vector3 targetPos, float maxYPos)
{
    Vector3 newVel = new Vector3();

    // 최고점까지의 시간
    float timeToMax = Mathf.Sqrt(-2 * (maxYPos - startPos.y) / Physics.gravity.y);
    // 최고점에서 골인 지점까지의 시간
    float timeToTargetY = Mathf.Sqrt(-2 * (maxYPos - targetPos.y) / Physics.gravity.y);
    float totalFlightTime = timeToMax + timeToTargetY;

    // 지면상의 방향과 거리 계산
    Vector3 direction = new Vector3(targetPos.x, 0f, targetPos.z) - 
                      new Vector3(startPos.x, 0f, startPos.z);
    float range = direction.magnitude;  // 여기서 지역 변수로 range 계산
    Vector3 unitDirection = direction.normalized;
    
    // 수평 방향 속도
    float horizontalVelocityMagnitude = range / totalFlightTime;
    
    // 각 축의 속도 계산
    newVel.x = horizontalVelocityMagnitude * unitDirection.x;
    newVel.z = horizontalVelocityMagnitude * unitDirection.z;
    // 수직 방향 속도
    newVel.y = Mathf.Sqrt(-2.0f * Physics.gravity.y * (maxYPos - startPos.y));

    return newVel;
}


public void ReleaseShot()
{
    Debug.Log("ReleaseShot Called"); // 디버그 로그 추가
    if(shouldReleaseShot)
    {
        Debug.Log("Shooting Ball"); // 디버그 로그 추가
        ShootBall();
        shouldReleaseShot = false;
    }
}

private bool IsFacingAwayFromHoop()
{
    if (hoopTransform == null) return false;

    Vector3 directionToHoop = hoopTransform.position - player.position;
    directionToHoop.y = 0; // 수평 방향만 고려
    
    // 플레이어의 전방 벡터
    Vector3 playerForward = player.forward;
    playerForward.y = 0;
    
    // 두 벡터 사이의 각도 계산
    float angle = Vector3.Angle(playerForward, directionToHoop);
    
    // 각도가 120도 이상이면 골대를 등지고 있다고 판단
    return angle > 120f;
}

void HandleShootInput()
{
    if (!hasBall) return;

    if (Input.GetKeyDown(KeyCode.E) && !isChargingShot)
    {
        // 골대를 등지고 있지 않을 때만 FaceHoop 실행
        if (!IsFacingAwayFromHoop())
        {
            FaceHoop();
        }
        
        animator.SetBool("IsShooting", true);
        StartCharging();
    }

    if (Input.GetKeyUp(KeyCode.E) && isChargingShot)
    {
        shouldReleaseShot = true;
        isChargingShot = false;
        animator.SetBool("IsShooting", false);
        
        // 공 소유권은 공이 실제로 발사된 후에 해제
        Invoke("ReleaseBallOwnership", 0.1f);
    }
}

void ReleaseBallOwnership()
{
    hasBall = false;
    isDribbling = false;
    animator.SetBool("HasBall", false);
}


    Vector3 CalculateShootDirection(Vector3 targetPos, float force)
{
    Vector3 toHoop = hoopTransform.position - transform.position;
    float distance = Vector3.Distance(transform.position, hoopTransform.position);
    
    float heightFactor = Mathf.Clamp(distance / 5f, 1f, 3f);
    
    Vector3 horizontalDir = new Vector3(toHoop.x, 0, toHoop.z).normalized;
    Vector3 upDir = Vector3.up * heightFactor;
    
    Vector3 finalDir = (horizontalDir + upDir).normalized;
    return finalDir;
}

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hoop") && canScore)
        {
            Debug.Log("Score!");
            if(scoreManager != null)
            {
                scoreManager.AddScore();
                canScore = false;
                Invoke("ResetScoring", 1f); // 1초 후 다시 득점 가능
            }
        }
    }
    void ResetScoring()
    {
        canScore = true;
    }

    void StartCharging()
{
    if (!isChargingShot)
    {
        isChargingShot = true;
        shootChargeStartTime = Time.time;

        
       // FaceHoop();

        // 드리블 상태 종료
        isDribbling = false;
        isMovingToHand = true;
        ballRigidbody.useGravity = false;

        // 드리블 애니메이션 종료
        animator.SetBool("IsDribbling", false);

        if(powerSlider != null)
        {
            powerSlider.gameObject.SetActive(true);
        }
    }
}

void FaceHoop()
{
    if (hoopTransform == null) return;

    // 골대 방향 계산 (Y축 고정)
    Vector3 directionToHoop = hoopTransform.position - player.position;
    directionToHoop.y = 0; // 수평 방향만 고려

    // 방향 벡터가 너무 작으면 실행하지 않음
    if (directionToHoop.sqrMagnitude < 0.01f) return;

    // 목표 Y축 회전 값 계산
    Quaternion targetRotation = Quaternion.LookRotation(directionToHoop);

    // Y축 회전만 변경 (Freeze Rotation Y는 물리적으로 유지)
    player.rotation = Quaternion.Euler(
        player.rotation.eulerAngles.x,     // 기존 X축 값 유지
        targetRotation.eulerAngles.y,     // 계산된 Y축 값
        player.rotation.eulerAngles.z     // 기존 Z축 값 유지
    );
}


  void ShootBall()
{
    // 초기화
    Vector3 startPosition = transform.position;
    Vector3 finalPosition = hoopTransform.position;
    float maxYPos = CalcMaxHeight(startPosition, finalPosition);
    
    // 발사 속도 계산
    Vector3 initialVelocity = CalculateShootVelocity(startPosition, finalPosition, maxYPos);
    
    // 차지 파워에 따른 속도 조절
    float chargeTime = Mathf.Min(Time.time - shootChargeStartTime, maxChargeTime);
    float chargePercent = chargeTime / maxChargeTime;
    initialVelocity *= Mathf.Lerp(0.8f, 1.2f, chargePercent);
    
    // 공 설정
    transform.SetParent(null);
    ballRigidbody.isKinematic = false;
    ballRigidbody.useGravity = true;
    
    // 발사
    ballRigidbody.linearVelocity = initialVelocity;
    
    // 회전 효과
    float spinForce = Mathf.Lerp(3f, 7f, chargePercent);
    ballRigidbody.AddTorque(Vector3.right * spinForce, ForceMode.Impulse);
    
    // 상태 초기화
    isChargingShot = false;
    isDribbling = false;
    isMovingToHand = false;
    animator.SetBool("IsDribbling", false);
    
    if(powerSlider != null)
    {
        powerSlider.gameObject.SetActive(false);
        powerSlider.value = 0;
    }
}


    // (선택적) 차지 파워를 시각적으로 표시하는 함수
    public float GetChargePercent()
    {
        if (!isChargingShot) return 0f;
        float chargeTime = Mathf.Min(Time.time - shootChargeStartTime, maxChargeTime);
        return chargeTime / maxChargeTime;
    }


      void Start()
    {
        InitializeComponents();
        InitializeState();
        playerInput = player.GetComponent<vThirdPersonInput>();
    }

    void InitializeComponents()
{
    Rigidbody rb = player.GetComponent<Rigidbody>();
    if (rb != null)
    {
        rb.angularVelocity = Vector3.zero;
    }

    if (ballRigidbody == null)
        ballRigidbody = GetComponent<Rigidbody>();

    playerController = player.GetComponent<CharacterController>();
    animator = player.GetComponent<Animator>();
    scoreManager = FindObjectOfType<ScoreManager>();

    // AudioSource 초기화
   if (dribbleAudioSource == null)
    {
        dribbleAudioSource = gameObject.AddComponent<AudioSource>();
        dribbleAudioSource.playOnAwake = false;
        dribbleAudioSource.spatialBlend = 1f;  // 3D 사운드로 설정
        dribbleAudioSource.minDistance = 1f;
        dribbleAudioSource.maxDistance = 15f;
        dribbleAudioSource.rolloffMode = AudioRolloffMode.Linear;
    }
}

    void InitializeState()
    {
        hasBall = false;
        isDribbling = false;
        isMovingToHand = false;

        if(ballRigidbody != null)
        {
            ballRigidbody.isKinematic = false;
            ballRigidbody.useGravity = true;
        }

        if(powerSlider != null)
        {
            powerSlider.gameObject.SetActive(false);
        }

        animator.SetBool("HasBall", false);
        animator.SetBool("IsDribbling", false);
    }

    void FixedUpdate()
    {
        UpdatePlayerSpeed();
        AdjustDribbleParameters();

        if (hasBall && isDribbling && !isPerformingDunk)
        {
            LimitBallHeight();           
            if (CheckCatchCondition())    
            {
                isDribbling = false;
                isMovingToHand = true;
                ballRigidbody.useGravity = false;
                ballRigidbody.linearVelocity = Vector3.zero;
                holdTimer = 0f;
            }
            else
            {
                HandleBallPosition();     
                CheckGroundAndBounce();   
            }
        }
        else if (isMovingToHand)
        {
            MoveToHand();
        }
    }



void UpdateCharacterMovement()
{
    // 캐릭터 움직임 관련 로직
    lastPlayerPosition = player.position;
}




    #region Player Speed & Dribble Parameter
    void UpdatePlayerSpeed()
    {
        float distanceMoved = Vector3.Distance(player.position, lastPlayerPosition);
        playerSpeed = distanceMoved / Time.fixedDeltaTime;
        lastPlayerPosition = player.position;
    }

   void AdjustDribbleParameters()
{
    float speedThreshold = 3f;
    bool isRunning = playerSpeed > speedThreshold || isSprinting; // sprint 상태도 체크

    // 기존 파라미터 조정
    upwardForce = isRunning ? runDribbleForce : walkDribbleForce;
    bounceInterval = isRunning ? runBounceInterval : walkBounceInterval;
    maxDistance = isRunning ? runMaxDistance : 1.5f;

    // 새로운 파라미터들을 클래스 레벨 변수에 할당
    currentCatchHeight = isRunning ? runCatchHeight : walkCatchHeight;
    currentMaxDribbleHeight = isRunning ? runMaxDribbleHeight : walkMaxDribbleHeight;
    currentForwardOffset = isRunning ? runForwardOffset : walkForwardOffset;
    currentSideOffset = isRunning ? runSideOffset : walkSideOffset;
    currentBallStickiness = isRunning ? runBallStickiness : walkBallStickiness;

    // 속도에 따른 보정값 적용 (sprint 상태일 때 더 큰 보정 적용)
    float speedMultiplier = isSprinting ? 
        Mathf.Clamp01(playerSpeed / 10f) * 1.5f : // sprint 시 더 큰 multiplier
        Mathf.Clamp01(playerSpeed / 8f);
        
    currentForwardOffset *= (1f + speedMultiplier);
    currentBallStickiness *= (1f + speedMultiplier * 0.5f);

    // catchHeight와 maxDribbleHeight도 업데이트
    catchHeight = currentCatchHeight;
    maxDribbleHeight = currentMaxDribbleHeight;
}
    #endregion

    #region Dribbling Core Logic
    void LimitBallHeight()
    {
        // 공이 너무 높게 올라가면 위치/속도 제한
        if (transform.position.y > maxDribbleHeight)
        {
            Vector3 pos = transform.position;
            pos.y = maxDribbleHeight;
            ballRigidbody.MovePosition(pos);

            Vector3 vel = ballRigidbody.linearVelocity;
            if (vel.y > 0)
                vel.y = 0f;
            ballRigidbody.linearVelocity = vel;
        }
    }

    bool CheckCatchCondition()
    {
        // 이동 중일 때는 캐치 조건을 더 엄격하게 적용
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool isMoving = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;

        if (isMoving)
        {
            // 이동 중일 때는 더 높은 높이에서만 캐치
            return (transform.position.y >= catchHeight + 0.1f &&
                    ballRigidbody.linearVelocity.y < -2f); // 더 빠른 하강 속도 필요
        }
        
        // 제자리에서는 기존 조건 유지
        return (transform.position.y >= catchHeight - 0.05f &&
                ballRigidbody.linearVelocity.y < 0);
    }

    void HandleBallPosition()
{
    // 카메라 기준 방향 계산
    Vector3 cameraForward = Camera.main.transform.forward;
    Vector3 cameraRight = Camera.main.transform.right;
    cameraForward.y = 0f;
    cameraRight.y = 0f;
    cameraForward.Normalize();
    cameraRight.Normalize();

    // 입력값에 따른 오프셋 계산
    float h = Input.GetAxis("Horizontal");
    float v = Input.GetAxis("Vertical");

    // 목표 위치 계산 (현재 클래스 레벨 변수 사용)
    Vector3 targetPosition = player.position + 
        (cameraForward * currentForwardOffset * v) +
        (cameraRight * currentSideOffset * h);
    
    // 손의 위치를 부분적으로 반영
    Vector3 handInfluence = rightHand.position;
    handInfluence.y = transform.position.y;
    targetPosition = Vector3.Lerp(targetPosition, handInfluence, 0.3f);
    
    targetPosition.y = transform.position.y;

    // 거리 체크 및 위치 보정
    float distance = Vector3.Distance(
        new Vector3(player.position.x, 0, player.position.z),
        new Vector3(transform.position.x, 0, transform.position.z)
    );

    if (distance > maxDistance)
    {
        Vector3 newPos = Vector3.Lerp(transform.position, targetPosition, 
            Time.fixedDeltaTime * currentBallStickiness);
        ballRigidbody.MovePosition(newPos);
    }
}

    // 바닥 충돌 및 바운스 처리
    void CheckGroundAndBounce()
{
    bool isGrounded = Physics.CheckSphere(transform.position, groundCheckRadius, groundLayer);
    Debug.Log($"isGrounded: {isGrounded}, nextBounceTime: {Time.time >= nextBounceTime}"); // 추가


    
    if (Time.time >= nextBounceTime)
    {
        PlayDribbleSound();  // 사운드 재생
    }

    if ( Time.time >= nextBounceTime)
    {
        
        // y속도를 0으로(바닥에 닿는 순간)
        Vector3 vel = ballRigidbody.linearVelocity;
        vel.y = 0f;
        ballRigidbody.linearVelocity = vel;
            
        // 드리블 사운드 재생
        

            float heightDiff = catchHeight - transform.position.y; 
            float speedMultiplier = 1f + (playerSpeed * 0.1f);
            float adjustedForce = upwardForce * (1 + heightDiff * 0.5f) * speedMultiplier;

            // 위로 튕기는 힘
            ballRigidbody.AddForce(Vector3.up * adjustedForce, ForceMode.Impulse);

            // 전방으로 살짝 힘
            if (playerSpeed > 0.1f)
            {
                float forwardPush = playerSpeed * 0.3f; 
                ballRigidbody.AddForce(player.forward * forwardPush, ForceMode.Impulse);
            }

            nextBounceTime = Time.time + bounceInterval;
        }
        else if (!isGrounded)
        {
            // 공이 공중에 있을 때 손과의 연결성 유지
            Vector3 handPos = rightHand.position;
            Vector3 ballPos = transform.position;
            
            // XZ 평면상의 거리만 계산
            float horizontalDist = Vector3.Distance(
                new Vector3(handPos.x, 0, handPos.z),
                new Vector3(ballPos.x, 0, ballPos.z)
            );

            // 거리가 멀어질수록 더 강한 보정력 적용
            float correctionStrength = Mathf.InverseLerp(maxDistance, maxDistance * 2f, horizontalDist);
            correctionStrength = correctionStrength * 0.5f; // 보정력을 50%로 제한

            if (horizontalDist > maxDistance)
            {
                Vector3 correction = (handPos - ballPos).normalized;
                correction.y = 0; // 수직 방향은 제외
                
                // 속도에 따라 보정력 조절
                float speedFactor = Mathf.Lerp(0.5f, 1.5f, playerSpeed / 8f);
                ballRigidbody.AddForce(correction * correctionStrength * speedFactor, ForceMode.VelocityChange);
            }
        }

        // 공의 최대 속도 제한
        if (ballRigidbody.linearVelocity.magnitude > maxVelocity)
        {
            ballRigidbody.linearVelocity = Vector3.ClampMagnitude(ballRigidbody.linearVelocity, maxVelocity);
        }
    }
    #endregion

    #region Move Ball To Hand
    void MoveToHand()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool isMoving = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;

        // 이동 중일 때는 약한 손 추적
        if (isMoving)
        {
            // 공과 손의 수평 거리 계산
            Vector3 ballXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 handXZ = new Vector3(rightHand.position.x, 0, rightHand.position.z);
            float horizontalDistance = Vector3.Distance(ballXZ, handXZ);

            // 수평 거리가 너무 멀어졌을 때만 약하게 보정
            if (horizontalDistance > maxDistance * 1.5f)
            {
                Vector3 targetPos = transform.position;
                targetPos.x = Mathf.Lerp(transform.position.x, rightHand.position.x, Time.fixedDeltaTime * 3f);
                targetPos.z = Mathf.Lerp(transform.position.z, rightHand.position.z, Time.fixedDeltaTime * 3f);
                ballRigidbody.MovePosition(targetPos);
            }

            // 드리블 상태로 전환
            StartDribbleDown();
            return;
        }

        // 제자리에서는 기존 로직 유지
        float moveSpeed = 15f;
        Vector3 newPos = Vector3.Lerp(transform.position, rightHand.position, Time.fixedDeltaTime * moveSpeed);
        ballRigidbody.MovePosition(newPos);

        float distance = Vector3.Distance(transform.position, rightHand.position);

        if (distance < 0.1f)
        {
            holdTimer += Time.fixedDeltaTime;
            if (holdTimer >= holdBallInHandDuration)
            
            {
                StartDribbleDown();
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    void StartDribbleDown()
    {
        StartDribbleAnimation(); // 애니메이터 Bool On
        isMovingToHand = false;
        isDribbling = true;
        ballRigidbody.useGravity = true;

        // 아래쪽으로 약간의 초기 속도
        ballRigidbody.linearVelocity = Vector3.down * 5f;

        // 다음 바운스 가능 시간 업데이트
        nextBounceTime = Time.time + bounceInterval;
    }
    #endregion

    #region Animation Helpers
    void StartDribbleAnimation()
    {
        animator.SetBool("IsDribbling", true);
    }

    void StopDribbleAnimation()
    {
        animator.SetBool("IsDribbling", false);
    }
    #endregion

    void StartDunk()
{
    isPerformingDunk = true;
    isDribbling = false;
    
    animator.SetTrigger("DunkTrigger");

    // 공 물리 설정
    ballRigidbody.isKinematic = true;
    ballRigidbody.useGravity = false;
    transform.SetParent(rightHand);
}

    void CheckDunkCondition()
{
    if(hasBall && isDribbling && !isChargingShot && !isPerformingDunk)
    {
        if(hoopTransform == null) return;

        // C 키를 누르면 덩크 시도
        if(Input.GetKeyDown(KeyCode.C))
        {
            float distanceToHoop = Vector3.Distance(
                new Vector3(player.position.x, 0, player.position.z),
                new Vector3(hoopTransform.position.x, 0, hoopTransform.position.z)
            );

            if(distanceToHoop <= dunkRange)
            {
                Vector3 directionToHoop = (hoopTransform.position - player.position).normalized;
                float dot = Vector3.Dot(player.forward, directionToHoop);
                
                // 달리고 있거나 스프린트 중일 때 덩크 가능
                if(isSprinting || playerSpeed > 5f)
                {
                    StartDunk();
                }
            }
        }
    }
}
public void OnDunkRelease()
{
    isPerformingDunk = false;
    hasBall = false;
    isDribbling = false;
    
    // 애니메이션 파라미터 초기화
    animator.SetBool("IsDribbling", false);
    animator.SetBool("HasBall", false);
    animator.ResetTrigger("DunkTrigger");
    
    // 골대 위치 계산
    Vector3 hoopPosition = hoopTransform.position;
    Vector3 directionToHoop = (hoopPosition - player.position).normalized;
    
    // 플레이어의 Rigidbody 또는 CharacterController 가져오기
    Rigidbody playerRb = player.GetComponent<Rigidbody>();
    CharacterController charController = player.GetComponent<CharacterController>();
    
    // 점프력과 전진력 설정
    float jumpForce = 8f;
    float forwardForce = 5f;
    
    // 플레이어를 골대 방향으로 이동
    Vector3 moveDirection = new Vector3(directionToHoop.x, 0.8f, directionToHoop.z).normalized;
    
    if (playerRb != null && !playerRb.isKinematic)
    {
        // Rigidbody를 사용하는 경우
        playerRb.linearVelocity = moveDirection * forwardForce + Vector3.up * jumpForce;
    }
    else if (charController != null)
    {
        // CharacterController를 사용하는 경우
        StartCoroutine(DunkMovement(moveDirection, forwardForce, jumpForce));
    }
    
    // 공 처리
    transform.parent = null;
    ballRigidbody.isKinematic = false;
    ballRigidbody.useGravity = true;
    
    // 공을 골대 방향으로 약간 위로 던지기
    Vector3 ballDirection = (hoopPosition - transform.position).normalized;
    ballRigidbody.linearVelocity = (ballDirection + Vector3.up * 0.5f) * 4f;
    
    // 덩크 후 착지를 위한 코루틴 시작
    StartCoroutine(DunkLanding());
}

private IEnumerator DunkMovement(Vector3 moveDirection, float forwardForce, float jumpForce)
{
    float dunkDuration = 0.5f; // 덩크 동작 지속 시간
    float elapsedTime = 0f;
    Vector3 initialPosition = player.position;
    
    while (elapsedTime < dunkDuration)
    {
        // 포물선 운동 계산
        float verticalMovement = jumpForce * (1 - (elapsedTime / dunkDuration));
        Vector3 movement = moveDirection * forwardForce * Time.deltaTime;
        movement.y = verticalMovement * Time.deltaTime;
        
        // CharacterController를 통한 이동
        playerController.Move(movement);
        
        elapsedTime += Time.deltaTime;
        yield return null;
    }
}

private IEnumerator DunkLanding()
{
    yield return new WaitForSeconds(0.7f); // 덩크 동작 후 대기
    
    // 착지 애니메이션 트리거 설정
    animator.SetTrigger("Landing");
    
    // 플레이어 상태 초기화
    isPerformingDunk = false;
    animator.SetBool("IsJumping", false);
}



    #region Public API
    // 외부에서 드리블 재시작을 위한 메서드
    public void RestartDribble()
    {
        ballRigidbody.isKinematic = false;
        ballRigidbody.useGravity = true;
        isDribbling = true;
        isMovingToHand = false;
        holdTimer = 0f;
    }

    
    private void PlayDribbleSound()
{
    if (dribbleAudioSource == null || dribbleSound == null || !canPlaySound) return;

    // 사운드 재생 제어
    canPlaySound = false;
    
    // 속도에 따른 볼륨 조절 (더 낮은 값으로 조정)
    float speedRatio = Mathf.Clamp01(playerSpeed / 8f);
    float volume = Mathf.Lerp(minVolume, maxVolume, speedRatio);
    
    // 피치 변화 (더 좁은 범위로 조정)
    float pitch = Mathf.Lerp(0.9f, 1.1f, speedRatio);
    
    dribbleAudioSource.volume = volume;
    dribbleAudioSource.pitch = pitch;
    dribbleAudioSource.PlayOneShot(dribbleSound, volume);

    // 짧은 딜레이 후 다시 사운드 재생 가능하도록 설정
    StartCoroutine(ResetSoundCooldown());
}

private IEnumerator ResetSoundCooldown()
{
    yield return new WaitForSeconds(bounceInterval * 0.8f);  // 바운스 간격보다 약간 짧게
    canPlaySound = true;
}

#endregion
}