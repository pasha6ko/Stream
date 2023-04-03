using System;
using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.Rendering.PostProcessing;
public class Gun : MonoBehaviour
{
    [SerializeField]Transform mainCamera;
    public  GunSettings settings;
    public bool autoReload;
    [SerializeField] ParticleSystem fireEffect;
    [Range(10f, 300f)][SerializeField] float rotationTime = 40f;
    [SerializeField] GameObject bullet,muzzle;
    
   
    
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


    
    private Vector3 originalPosition;
    void Start()    
    {
        
        magazin = settings.magazineLimit;

        originalPosition = transform.localPosition ;

    }
    float fireTime = 0;
    
    public int magazin;
    [SerializeField] Transform AimPoint;
    
    void Update()
    {
        /*
        if (Input.GetMouseButtonDown(0) && state!= GunStates.Reload)
        {
            ChangeGunState(GunStates.Fire);

        }
        if (Input.GetMouseButtonUp(0) && state != GunStates.Reload)
        {
            ChangeGunState(GunStates.Idle);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ChangeGunState(GunStates.Reload);
        }*/
            
    }
    
    
    

    private void FixedUpdate()
    {//Переписатьы
        /*
        if (state == GunStates.Fire && fireTime >= lastFireTime + (1 / settings.fireSpeed) && magazin>0)
        {
            Fire();
            ChangeAnimationState(gunAnimator,GUN_FIRE,true,settings.fireSpeed); 
            ChangeAnimationState(playerAnimator, PLAYER_FIRE, true, settings.fireSpeed);
        }
        fireTime+= Time.deltaTime;*/

    }
    public void Reload()
    {
        
        magazin = settings.magazineLimit;
        print("reloaded");
        

    }
    DateTime testCounter;
    

    public void Fire()
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
            NetworkManager network =FindObjectOfType<NetworkManager>();
            if (hit.transform.GetComponent<PlayerController>() != null)
            {
                hit.transform.GetComponent<PlayerController>().hp -= 10;
            }
            Rigidbody cloneRb = hit.transform.GetComponent<Rigidbody>();
            if (cloneRb!=null)
            {
                cloneRb.AddExplosionForce(settings.bulletSpeed*10f,hit.point,1f);
            }
            
        }
        StartCoroutine(Recoil(settings.recoilAngle, settings.recoilSpeed));



        magazin--;
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
