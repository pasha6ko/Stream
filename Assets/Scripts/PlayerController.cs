using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class PlayerController : NetworkBehaviour
{

    public int hp;
    public Gun gun;
    [SerializeField]Transform camera;
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
    #endregion
    public enum GunStates
    {
        Fire,
        Idle,
        Reload,
        //void ChangeState(GunSettings)
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
            JumpsCount = MaxJumps;
            gunAnimator = gun.transform.GetComponent<Animator>();
        }
        else
        {
            Destroy(camera.transform.GetComponent<PostProcessLayer>());
            Destroy(camera.GetComponent<Camera>());
        }
        
    }

    

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            if (Input.GetMouseButtonDown(1))
            {
                Time.timeScale = 0.2f;
            }
            if (Input.GetMouseButtonUp(1))
            {
                Time.timeScale = 1f;
            }
            #region Rotation
            float eulerX = (camera.transform.rotation.eulerAngles.x + -Input.GetAxis("Mouse Y") * sensivity) % 360;
            float eulerY = (transform.rotation.eulerAngles.y + Input.GetAxis("Mouse X") * sensivity) % 360;
            camera.transform.rotation = Quaternion.Euler(eulerX, eulerY, camera.transform.rotation.eulerAngles.z);
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
        Quaternion LastCameraRot = camera.transform.rotation;
        Quaternion TargerCameraRot = Quaternion.EulerAngles(camera.transform.rotation.eulerAngles.x, camera.transform.rotation.eulerAngles.y, degrees);
        float value = 0f;
        print("Degr : " +degrees);
        while(value<=1f)
        {
            value += Time.deltaTime * RotationSpeed;
            Mathf.Max(value, 1);
            yield return null;
        }
        print(camera.transform.rotation.eulerAngles.z);
        yield return null;
    }
    public void ChangeState(States state)
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
    
    public void ChangeCameraRot(float Degrees)
    {
        print("Degrees "+ Degrees);
        StartCoroutine(CameraRotaiting(Degrees));
    }
    public void ReGenJumps()
    {
        JumpsCount = MaxJumps;
    }
    IEnumerator ReloadAnimation()
    {
        ChangeAnimationState(gunAnimator, GUN_RELOAD, true);
        ChangeAnimationState(playerAnimator, PLAYER_RELOAD, true);
        gun.magazin = gun.settings.magazineLimit;
        print("reloaded");
        ChangeGunState(GunStates.Idle);
        yield return null;
    }
    float lastFireTime = 0;
    public void ChangeGunState(GunStates state)
    {
        this.gunState = state;
        //fireTime = 0;
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
    IEnumerator FireControle()
    {
        while (Input.GetMouseButton(0))
        {
            gun.Fire();
            Debug.Log("Fire");
            GameObject clone = Instantiate(pref);
            NetworkServer.Spawn(clone);
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
    private void OnCollisionEnter(Collision collision)
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
}
