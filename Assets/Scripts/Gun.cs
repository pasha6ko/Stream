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
    
   
    void Start()    
    {
    }
    float fireTime = 0;
    
    public int magazin;
    [SerializeField] Transform AimPoint;
    public void Reload()
    {
        
        magazin = settings.magazineLimit;
        print("reloaded");
        

    }
    DateTime testCounter;



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
