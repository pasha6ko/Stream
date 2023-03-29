using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Camera camera;
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
    public enum States
    {
        Stay,
        Walk,
        Run,
        WallRun,
        Fall
    }
    public States PlayerState;
    float RotID;

    Animator playerAnimator;
    const string PLAYER_RUN = "Run";
    // Start is called before the first frame update
    void Start()
    {
        PlayerState = States.Walk;
        Cursor.lockState = CursorLockMode.Locked;
        camera = Camera.main;
        rb = GetComponent<Rigidbody>();
        JumpsCount = MaxJumps;
        RotID = 0f;
        isRotating = false;
        playerAnimator = GetComponent<Animator>();
        
    }

    Quaternion TargerCameraRot, LastCameraRot;
    bool isRotating;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Time.timeScale = 0.2f;
        }
        if (Input.GetMouseButtonUp(1))
        {
            Time.timeScale = 1f;
        }
        if (isRotating)
        {
            print("Rotating");
            RotID+=Time.deltaTime*RotationSpeed;
            camera.transform.rotation = Quaternion.Euler(camera.transform.rotation.eulerAngles.x, camera.transform.rotation.eulerAngles.y, Quaternion.Lerp(LastCameraRot,TargerCameraRot,RotID).eulerAngles.z);
            if (RotID >= 1f)
            {
                isRotating = false;
                RotID=0;
            }
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
            rb.velocity+= transform.right * speed* HMove;
            rb.velocity += transform.up * VerticalVelocity;
            if (VMove > 0)
            {

            }
            /*
            transform.position += transform.forward * VMove * speed * Time.deltaTime;
            transform.position += transform.right * HMove * speed * Time.deltaTime;*/
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            
            ChangeState(States.Fall);
            rb.velocity+=transform.up * JumpForce;
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
                rb.AddForce(6*Camera.main.transform.forward);

            }
            else
            {
                IBetweenPoints += Time.deltaTime * WRSpeed;
                Vector3 WRMoveDiraction = (WRLineData[1]-transform.position).normalized;
                
                transform.position = Vector3.Lerp(WRLineData[0], WRLineData[1], IBetweenPoints);
                if (IBetweenPoints >= 1)
                {
                    IBetweenPoints = 0;
                    WRLineData.RemoveAt(0);
                }
            }     
        }
        #endregion
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
        LastCameraRot = camera.transform.rotation;
        TargerCameraRot = Quaternion.Euler(0, 0, Degrees);
        isRotating = true;
        
    }
    public void ReGenJumps()
    {
        JumpsCount = MaxJumps;
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