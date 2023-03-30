using System;
using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.Rendering.PostProcessing;
public class Gun : MonoBehaviour
{
    [SerializeField]Transform mainCamera;
    [SerializeField] GunSettings settings;
    public bool autoReload;
    [SerializeField] ParticleSystem fireEffect;
    [Range(10f, 300f)][SerializeField] float rotationTime = 40f;
    [SerializeField] GameObject bullet,muzzle;
    [SerializeField] AnimationClip reloadAnim;
    [SerializeField] Animator playerAnimator;
   
    
    #region AnimationSet
    // Оружие
    
    const string PLAYER_FIRE = "ManFire";
    const string PLAYER_RELOAD = "ManReload";
    //игрок
    [SerializeField] Animator gunAnimator;
    const string GUN_RELOAD = "GunReload";
    const string GUN_FIRE = "GunFire";

    #endregion

    private void Awake()
    {
        
    }


    enum GunStates
    {
        Fire,
        Idle,
        Reload,
        //void ChangeState(GunSettings)
    }
    GunStates state;
    
    private Vector3 originalPosition;
    void Start()    
    {
        
        //gunAnimator = GetComponent<Animator>();
        magazin = settings.magazineLimit;
        state = GunStates.Idle;
        //transform.LookAt(AimPoint.position);
        originalPosition = transform.localPosition ;

    }
    float fireTime = 0;
    float lastFireTime = 0;
    int magazin;
    [SerializeField] Transform AimPoint;
    string currentAnimaton;
    void Update()
    {
        #region GunRotationAnimation
        if (state != GunStates.Reload)
        {
        }
        #endregion
        if (Input.GetMouseButtonDown(0) && state!= GunStates.Reload)
        {
            ChangeState(GunStates.Fire);

        }
        if (Input.GetMouseButtonUp(0) && state != GunStates.Reload)
        {
            ChangeState(GunStates.Idle);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ChangeState(GunStates.Reload);
        }
        //Debug.DrawRay(muzzle.transform.position, muzzle.transform.forward,Color.red);
            
    }
    void ChangeState(GunStates state)
    {
        this.state = state;
        fireTime = 0;
        switch (state)
        {
            case GunStates.Fire:
                print("fire");
                lastFireTime = 0;
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
    IEnumerator ReloadAnimation()
    {
        ChangeAnimationState(gunAnimator,GUN_RELOAD,true);
        ChangeAnimationState(playerAnimator,PLAYER_RELOAD,true);
        magazin = settings.magazineLimit;
        print("reloaded");
        ChangeState(GunStates.Idle);
        yield return null;
    }
    

    private void FixedUpdate()
    {
        if (state == GunStates.Fire && fireTime >= lastFireTime + (1 / settings.fireSpeed) && magazin>0)
        {
            Fire();
            ChangeAnimationState(gunAnimator,GUN_FIRE,true,settings.fireSpeed); 
            ChangeAnimationState(playerAnimator, PLAYER_FIRE, true, settings.fireSpeed);
        }
        fireTime+= Time.deltaTime;

    }
    void Reload()
    {
        ChangeAnimationState(gunAnimator, GUN_RELOAD, true);
        ChangeAnimationState(playerAnimator, PLAYER_RELOAD, true);
        magazin = settings.magazineLimit;
        print("reloaded");
        ChangeState(GunStates.Idle);

    }
    DateTime testCounter;
    

    void Fire()
    {
        // Calculate bullet direction
        Vector3 direction =mainCamera.transform.forward;
       
        
        direction = Quaternion.Euler(settings.recoilAngle, UnityEngine.Random.RandomRange(-settings.FireRange, settings.FireRange) , 0) * direction;

        Ray ray = new Ray(mainCamera.transform.position, direction);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {

            GameObject bulletHole = Instantiate(settings.bulletHole[0], hit.point + hit.normal * 0.001f, Quaternion.LookRotation(hit.normal),hit.transform);// добавить разные вариации
            Destroy(bulletHole, 25);
            bulletHole.transform.localScale = settings.bulletHole[0].transform.localScale;
            Rigidbody cloneRb = hit.transform.GetComponent<Rigidbody>();
            if (cloneRb!=null)
            {
                cloneRb.AddExplosionForce(settings.bulletSpeed*10f,hit.point,1f);
            }
            
        }
        ChangeAnimationState(playerAnimator,PLAYER_FIRE);


        StartCoroutine(Recoil(settings.recoilAngle, settings.recoilSpeed));



        magazin--;
        if (magazin <= 0 && autoReload)
        {
            ChangeState(GunStates.Reload);
        }
        
        testCounter = DateTime.Now;
        lastFireTime += 1/ settings.fireSpeed;
    }
    float currentAngle;
    private IEnumerator Recoil(float angle, float speed)
    {
        currentAngle -= angle;
        mainCamera.transform.localRotation = Quaternion.Euler(mainCamera.transform.localRotation.eulerAngles.x + currentAngle, 0, 0);
        while (currentAngle < 0)
        {
            
            currentAngle += speed * Time.deltaTime;
            currentAngle = Mathf.Min(currentAngle, 0);
            mainCamera.transform.localRotation = Quaternion.Euler(mainCamera.transform.localRotation.eulerAngles.x+ speed * Time.deltaTime, 0, 0);
            yield return null;
        }
    }
    void ChangeAnimationState(Animator animator,string newAnimation, bool ignoreCurrent = false,float speed =1f)
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

}

[System.Serializable]
public class GunSettings
{
    public string name;
    [Range(0,300f)]public float fireSpeed, bulletSpeed,bodyDamage,headDamage,coolingTime, reloadTime,switchTime,FireRange,crouchFireRange,recoilAngle,recoilSpeed;
    public int magazineLimit;
    public bool Holding, AlternativeShooting;
    public GameObject[] bulletHole;
}
