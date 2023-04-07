using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class PlayerController : NetworkBehaviour
{

    public int hp;
    public Gun gun;
    [SerializeField]Transform mainCamera;
    Camera cam;
    [Range(0.1f, 10f)] public float sensivity;
    [Range(0.1f ,1000f)] public float StandartSpeed,RotationSpeed;
    [Range(0.1f, 2000f)] public float JumpForce;
    [SerializeField] LayerMask layerGround;
    [SerializeField] int MaxJumps;
    public List<Vector3> WRLineData = new List<Vector3>();
    int JumpsCount;
    [Range(0, 200f)] public float WRSpeed;
    float IBetweenPoints;
    Rigidbody rb;
    [SerializeField] LayerMask _groundMask, _wallMask;
    #region AnimationSet
    //игрок
    [SerializeField] Animator playerAnimator;
    const string PLAYER_FIRE = "ManFire";
    const string PLAYER_RELOAD = "ManReload";
    //оружие
    Animator gunAnimator;
    const string GUN_RELOAD = "GunReload";
    const string GUN_FIRE = "GunFire";
    NetworkManager networkManager;
    [SerializeField] GameObject Hole;
    [SerializeField] GameObject AimUI;
    [SerializeField] int AimFOV, Fov;
    #endregion
    public enum GunStates
    {
        Fire,
        Idle,
        Reload,
    }
    GunStates gunState;

    public enum States
    {
        Stay,
        Walk,
        Run,
        WallRun,
        Fall
    }
    public States PlayerState;
    #region GunParams
    private Vector3 originalPosition;
    public bool autoReload;
    [SerializeField] ParticleSystem fireEffect;
    [Range(10f, 300f)][SerializeField] float rotationTime = 40f;
    [SerializeField] GameObject bullet, muzzle;

    public int magazin;
    #endregion
    // Start is called before the first frame update
    void Start()
    {
        hpCounter = GameObject.Find("Canvas/HP").transform.GetComponent<TMPro.TextMeshProUGUI>();

        hp = 100;
        if (isLocalPlayer)
        {
            networkManager = FindObjectOfType<NetworkManager>();
            PlayerState = States.Walk;
            Cursor.lockState = CursorLockMode.Locked;
            gunState = GunStates.Idle;
            rb = GetComponent<Rigidbody>();
            cam = Camera.main.GetComponent<Camera>();
            JumpsCount = MaxJumps;
            gunAnimator = gun.transform.GetComponent<Animator>();
            AimUI = GameObject.Find("Canvas/Aim");
            AimUI.SetActive(false);
        }
        else
        {
            Destroy(mainCamera.transform.GetComponent<PostProcessLayer>());
            Destroy(mainCamera.GetComponent<Camera>());
        }
        magazin = gun.settings.magazineLimit;

        originalPosition = transform.localPosition;

    }

    bool isAiming;

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            #region Rotation
            float eulerX = (mainCamera.transform.rotation.eulerAngles.x + -Input.GetAxis("Mouse Y") * sensivity) % 360;
            float eulerY = (transform.rotation.eulerAngles.y + Input.GetAxis("Mouse X") * sensivity) % 360;
            mainCamera.transform.rotation = Quaternion.Euler(eulerX, eulerY, mainCamera.transform.rotation.eulerAngles.z);
            transform.rotation = Quaternion.Euler(0, eulerY, 0);
            #endregion

            #region Movement

            float speed = StandartSpeed;

            if (Input.GetKey(KeyCode.LeftShift))
            {

                speed = StandartSpeed * 1.3f;
            }

            float HMove = Input.GetAxisRaw("Horizontal");
            float VMove = Input.GetAxisRaw("Vertical");

            if (PlayerState != States.WallRun)
            {
                float VerticalVelocity = rb.velocity.y;
                rb.velocity = transform.forward * VMove * speed;
                rb.velocity += transform.right * speed * HMove;
                rb.velocity += transform.up * VerticalVelocity;
                if (VMove > 0)
                {

                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {

                ChangeState(States.Fall);
                rb.velocity += transform.up * JumpForce;
                print("Jump");
                ReGenJumps();


            }

            if (rb.velocity.y != 0 && Input.GetKeyDown(KeyCode.LeftControl))
            {
                if (PlayerState == States.WallRun)
                {
                    ChangeState(States.Fall);
                }

                rb.AddForce(transform.up * -JumpForce * 2);

            }


            #endregion

            #region WallWalk

            if (PlayerState != States.WallRun)
            {
                WRLineData.Clear();
            }
            else
            {
                if (WRLineData.Count <= 2)
                {
                    print("Fall");
                    ChangeState(States.Fall);
                    rb.AddForce(6 * Camera.main.transform.forward);

                }
                else
                {
                    IBetweenPoints += Time.deltaTime * WRSpeed;
                    Vector3 WRMoveDiraction = (WRLineData[1] - transform.position).normalized;

                    transform.position = Vector3.Lerp(WRLineData[0], WRLineData[1], IBetweenPoints);
                    if (IBetweenPoints >= 1)
                    {
                        IBetweenPoints = 0;
                        WRLineData.RemoveAt(0);
                    }
                }
            }
            #endregion

            #region GunControl
            if (Input.GetMouseButtonDown(0) && gunState != GunStates.Reload)
            {
                ChangeGunState(GunStates.Fire);

            }
            if (Input.GetMouseButtonUp(0) && gunState != GunStates.Reload)
            {
                ChangeGunState(GunStates.Idle);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                ChangeGunState(GunStates.Reload);
            }
            if (Input.GetMouseButton(1) && gunState != GunStates.Reload && !isAiming)
            {
                AimUI.SetActive(true);
                cam.fieldOfView = AimFOV;
                isAiming = true;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                AimUI.SetActive(false);
                cam.fieldOfView = Fov;
                isAiming=false;
            }
            #endregion
        }
    }
    TMPro.TextMeshProUGUI hpCounter;
    private void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            hpCounter.text = hp.ToString();
        }
    }
    IEnumerator CameraRotaiting(float degrees)
    {
        Quaternion LastCameraRot = mainCamera.transform.rotation;
        Quaternion TargerCameraRot = Quaternion.EulerAngles(mainCamera.transform.rotation.eulerAngles.x, mainCamera.transform.rotation.eulerAngles.y, degrees);
        float value = 0f;
        print("Degr : " +degrees);
        while(value<=1f)
        {
            value += Time.deltaTime * RotationSpeed;
            Mathf.Max(value, 1);
            yield return null;
        }
        print(mainCamera.transform.rotation.eulerAngles.z);
        yield return null;
    }
    [Client]public void ChangeState(States state)
    {
        States LastState = PlayerState;
        PlayerState = state;
        switch (state)
        {
            case States.WallRun:
                WRLineData.Insert(0,transform.position);
                ReGenJumps();
                break;
            case States.Fall:
                if (LastState == States.WallRun)
                {
                    ChangeCameraRot(0);
                }
                    
                Transform RenderLinesParent = GameObject.Find("WRLines").transform;
                rb.velocity -= new Vector3(0, rb.velocity.y, 0);
                for (int i = 0; i < RenderLinesParent.childCount; i++)
                {
                    Destroy(RenderLinesParent.GetChild(i).gameObject);
                }
                
                WRLineData.Clear();
                break;

            default:
                Debug.LogWarning("This state does not exist");
                break;
        }
    }
    
    [Client]public void ChangeCameraRot(float Degrees)
    {
        print("Degrees "+ Degrees);
        StartCoroutine(CameraRotaiting(Degrees));
    }
    public void ReGenJumps()
    {
        JumpsCount = MaxJumps;
    }
    [Client]IEnumerator ReloadAnimation()
    {
        ChangeAnimationState(gunAnimator, GUN_RELOAD, true);
        ChangeAnimationState(playerAnimator, PLAYER_RELOAD, true);
        gun.magazin = gun.settings.magazineLimit;
        print("reloaded");
        ChangeGunState(GunStates.Idle);
        yield return null;
    }
    float lastFireTime = 0;
    [Client] public void ChangeGunState(GunStates state)
    {
        this.gunState = state;
        switch (state)
        {
            case GunStates.Fire:
                print("fire");
                StartCoroutine(FireControle());
                break;
            case GunStates.Idle:
                print("idle");
                break;
            case GunStates.Reload:
                Reload();
                print("reload");
                break;
        }
    }
    void Reload()
    {
        ChangeAnimationState(gunAnimator, GUN_RELOAD, true);
        ChangeAnimationState(playerAnimator, PLAYER_RELOAD, true);
        gun.Reload();
        ChangeGunState(GunStates.Idle);
    }
    [SerializeField] GameObject pref;
    
    private IEnumerator FireControle()
    {
        while (Input.GetMouseButton(0))
        {
            ClientFire();
            Debug.Log("Fire");
            
            ChangeAnimationState(gunAnimator, GUN_FIRE, true, gun.settings.fireSpeed);
            ChangeAnimationState(playerAnimator, PLAYER_FIRE, true, gun.settings.fireSpeed);
            
            yield return new WaitForSeconds(1/gun.settings.fireSpeed);
        }
    }
    
    string currentAnimaton;
    void ChangeAnimationState(Animator animator, string newAnimation, bool ignoreCurrent = false, float speed = 1f)
    {
        if (currentAnimaton == newAnimation && !ignoreCurrent)
        {
            return;
        }
        animator.speed = speed;
        animator.Play(newAnimation);
        currentAnimaton = "Idle";
        print("Animation Played");
    }
    [Client]private void OnCollisionEnter(Collision collision)
    {
        //print("on colison");
        if (Mathf.Pow(2, collision.collider.gameObject.layer) == _groundMask.value)
        {
            JumpsCount = MaxJumps;
        }
        if (Mathf.Pow(2, collision.collider.gameObject.layer) == _wallMask.value)
        {
            
        }
    }
    #region GunDunctions
    [Client]
    void ClientFire()
    {
        Vector3 direction = mainCamera.transform.forward;
        direction = Quaternion.Euler(gun.settings.recoilAngle, UnityEngine.Random.Range(-gun.settings.FireRange, gun.settings.FireRange), 0) * direction;
        Ray ray = new Ray(mainCamera.transform.position, direction);
        Debug.DrawRay(mainCamera.transform.position, direction, Color.green);
       // Debug.LogError("Wait");
        Fire(ray);
        StartCoroutine(Recoil(gun.settings.recoilAngle, gun.settings.recoilSpeed));
        magazin--;
    }
    [Command]
    private void Fire(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            GameObject bulletHole = Instantiate(Hole, hit.point + hit.normal * 0.001f, Quaternion.LookRotation(hit.normal)) as GameObject ;// добавить разные вариации
            Destroy(bulletHole, 25);
            NetworkServer.Spawn(bulletHole);
            if (hit.transform.GetComponent<PlayerController>() != null)
            {
                PlayerController player = hit.transform.GetComponent<PlayerController>();
                GetDamage(player, 10);
                Debug.Log("Hit : "+ player.hp);
            }
            Rigidbody cloneRb = hit.transform.GetComponent<Rigidbody>();
            if (cloneRb != null)
            {
                cloneRb.AddExplosionForce(gun.settings.bulletSpeed * 10f, hit.point, 1f);
            }
            NetworkServer.SpawnObjects();
        } 
    }
    [ClientRpc]
    void GetDamage(PlayerController player, int damage)
    {
        player.hp -= damage;
    }

    float currentAngle;
    private IEnumerator Recoil(float angle, float speed)
    {
        currentAngle -= angle;
        mainCamera.transform.localRotation = Quaternion.Euler(mainCamera.transform.localRotation.eulerAngles.x + currentAngle, 0, 0);
        while (currentAngle < 0 )
        {

            currentAngle += speed * Time.deltaTime;
            currentAngle = Mathf.Min(currentAngle, 0);
            mainCamera.transform.localRotation = Quaternion.Euler(mainCamera.transform.localRotation.eulerAngles.x + speed * Time.deltaTime, 0, 0);
            yield return null;
        }
    }
    #endregion

}
