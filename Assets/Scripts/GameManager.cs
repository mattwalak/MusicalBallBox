using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameObject sampleBall;
    public LaunchArrow launchArrow;
    public SoundManager soundManager;

    public PhysicsMaterial2D horizontalMaterial;
    public PhysicsMaterial2D verticalMaterial;

    public BoxCollider2D topWall, bottomWall, rightWall, leftWall;

    public Slider gravitySlider;
    public Slider horizontalBounceSlider;
    public Slider verticalBounceSlider;

    public static float MAX_GRAVITY = -40;
    public static float MIN_GRAVITY = 0;

    public static float COLLISION_ELASTICITY = 1.0f;
    public static float VELOCITY_MULTIPLIER = 5.0f;
    public static float ENERGY_TOP = 8 * 9.81f;
    public static float ENERGY_BOTTOM = 3.0f;
    public static float VELOCITY_EPSILON = 0.0001f;

    private float screenWidth, screenHeight;
    public static float BOX_WIDTH = 4f;
    public static float BOX_HEIGHT = 4f;
    public static float BALL_RADIUS = 0.2f;

    private float IMPULSE_RADIUS = 30.0f;

    private bool isInTouch = false;
    private Vector2 touchStart;
    private Camera mainCamera;

    private bool firstUpdate = true;


    void Start(){
        screenHeight = Camera.main.orthographicSize;
        screenWidth = screenHeight * Camera.main.aspect;
        mainCamera = Camera.main;
    }

    void Update(){
        if(firstUpdate){
            firstUpdate = false;
            UpdateGravity();
            UpdateHorizontalBounce();
            UpdateVerticalBounce();
        }

        if(Input.GetMouseButtonDown(0)){
            Vector2 pos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            bool isInBox = (pos.x > - BOX_WIDTH && pos.x < BOX_WIDTH && pos.y > - BOX_HEIGHT && pos.y < BOX_HEIGHT);

            if(!isInTouch && isInBox){
                isInTouch = true;
                touchStart = pos;
                launchArrow.Show();
            }
        }

        if(Input.GetMouseButton(0)){
            launchArrow.SetData(touchStart, mainCamera.ScreenToWorldPoint(Input.mousePosition));
        }


        if(Input.GetMouseButtonUp(0)){
            if(isInTouch){
                LaunchBall(touchStart, mainCamera.ScreenToWorldPoint(Input.mousePosition));
            }

            isInTouch = false;
            launchArrow.Hide();
        }
    }

    public void LaunchBall(Vector2 start, Vector2 end){
        Vector2 launchVel = end - start;
        launchVel = launchVel * VELOCITY_MULTIPLIER;

        GameObject obj = Instantiate(sampleBall, transform);
        obj.transform.position = start;

        SampleBall ball = obj.GetComponent<SampleBall>();
        ball.Init(launchVel);
    }

    public void AddBall(){
        Vector2 randomPosition = new Vector2(
            Random.Range(-1f, 1f) * (BOX_WIDTH - BALL_RADIUS),
            Random.Range(-1f, 1f) * (BOX_HEIGHT - BALL_RADIUS)
        );

        Vector2 randomImpulse = new Vector2(
            Random.Range(-1f, 1f) * IMPULSE_RADIUS,
            Random.Range(-1f, 1f) * IMPULSE_RADIUS
        );

        GameObject obj = Instantiate(sampleBall, transform);
        obj.transform.position = randomPosition;

        SampleBall ball = obj.GetComponent<SampleBall>();
        ball.Init(randomImpulse);
    }

    public void UpdateGravity(){
        Physics2D.gravity = new Vector2(0, Mathf.Lerp(MIN_GRAVITY, MAX_GRAVITY, gravitySlider.value));

        float gravity_Y = Physics2D.gravity.y;
        ENERGY_TOP = -2.0f * BOX_HEIGHT * gravity_Y;
        ENERGY_BOTTOM = gravity_Y / 3.0f;

        if(gravity_Y == 0){
            ENERGY_TOP = 1f;
            ENERGY_BOTTOM = 0f;
        }
    }

    public void UpdateHorizontalBounce(){
        horizontalMaterial.bounciness = horizontalBounceSlider.value;
        topWall.enabled = false;
        bottomWall.enabled = false;
        topWall.enabled = true;
        bottomWall.enabled = true;
    }

    public void UpdateVerticalBounce(){
        verticalMaterial.bounciness = verticalBounceSlider.value;
        rightWall.enabled = false;
        leftWall.enabled = false;
        rightWall.enabled = true;
        leftWall.enabled = true;
    }

    public void OnClearBox(){
        foreach(Transform child in transform){
            child.gameObject.GetComponent<SampleBall>().DestroySelf();
        }

        soundManager.KillAll();
    }   
}
