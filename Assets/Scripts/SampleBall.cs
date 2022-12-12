using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleBall : MonoBehaviour
{
    private Rigidbody2D rb2D;
    private SpriteRenderer sprite;
    private SoundManager soundManager;

    private int ID;

    private float screenHeight, screenWidth;

    public void Init(Vector2 initialImpulse)
    {
        screenHeight = Camera.main.orthographicSize;
        screenWidth = screenHeight * Camera.main.aspect;

        rb2D = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        soundManager = (SoundManager) FindObjectOfType(typeof(SoundManager));

        rb2D.velocity = initialImpulse;

        Vector2 normCoords = GetNormalizedCoords();
        float normEnergy = GetNormalizedEnergy();
        double rate = GetSampleRateFromCurrentVelocity();
        ID = soundManager.OpenNewVoice(normCoords.x, normCoords.y, rate, normEnergy);
        
    }

    // Garunteed to be between 0 and 1
    private Vector2 GetNormalizedCoords(){
        Vector2 normCoords = new Vector2(
            transform.position.x / (GameManager.BOX_WIDTH - GameManager.BALL_RADIUS),
            transform.position.y / (GameManager.BOX_HEIGHT - GameManager.BALL_RADIUS)
        );

        normCoords = (normCoords + new Vector2(1, 1))/2f;
        normCoords = new Vector2(
            Mathf.Clamp(normCoords.x, 0.0f, 1.0f),
            Mathf.Clamp(normCoords.y, 0.0f, 1.0f)
        );

        return normCoords;
    }

    // Garunteed to be between 0 and 1
    private float GetNormalizedEnergy(){
        float energy = GetTotalEnergy();
        energy = (energy - GameManager.ENERGY_BOTTOM) / (GameManager.ENERGY_TOP - GameManager.ENERGY_BOTTOM);
        energy = Mathf.Clamp(energy, 0.0f, 1.0f);

        return energy;
    }

    private void UpdateColor(float normX, float normY, float normEnergy){
        float hue = normX / 2f;
        float saturation = (3f * normY / 4f) + 0.25f;
        float opacity = Mathf.Pow(normEnergy, 0.5f);

        Color col = Color.HSVToRGB(hue, saturation, 1.0f);
        col = new Color(col.r, col.g, col.b, opacity);
        sprite.material.color = col;
    }

    public void Update(){
        // Update Color
        Vector2 normCoords = GetNormalizedCoords();
        float normEnergy = GetNormalizedEnergy();

        UpdateColor(normCoords.x, normCoords.y, normEnergy);

        soundManager.UpdateModulatedData(ID, normCoords.x, normCoords.y, normEnergy);
        // Debug.Log("Energy = "+normEnergy);
        
        // Destroy if energy is too low
        if(normEnergy <= 0){
            DestroySelf();
        }

        // Destroy if x velocity is effectively 0
        if(Mathf.Abs(rb2D.velocity.x) < GameManager.VELOCITY_EPSILON){
            // DestroySelf();
        }

        // Destroy if somehow escaped
        if(transform.position.x < -screenWidth || transform.position.x > screenWidth || 
           transform.position.y < -screenHeight || transform.position.y > screenHeight){
            DestroySelf();
        }
    }

    public void DestroySelf(){
        soundManager.FreeVoice(ID);
        Destroy(gameObject);
    }

    private float GetTotalEnergy(){
        // Kinetic
        float velocity = Mathf.Sqrt(
            Mathf.Pow(rb2D.velocity.x, 2) + Mathf.Pow(rb2D.velocity.y, 2)
        );
        float kineticEnergy = 0.5f * rb2D.mass * Mathf.Pow(velocity, 2);

        // Potential
        float height = transform.position.y - (-(GameManager.BOX_HEIGHT - GameManager.BALL_RADIUS));
        float gravity = -Physics2D.gravity.y * rb2D.gravityScale;
        float potentialEnergy = rb2D.mass * gravity * height;

        return kineticEnergy + potentialEnergy;
    }

    private double GetSampleRateFromCurrentVelocity(){
        double xVelocity = rb2D.velocity.x;
        double travelTime = 2 * GameManager.BOX_WIDTH / xVelocity;
        double rate = soundManager.GetSampleLength() / travelTime;
        return rate;
    }

    public void OnCollisionEnter2D(Collision2D collisionInfo)
    {
        if(collisionInfo.gameObject.CompareTag("Vertical_Wall")){
            double rate = GetSampleRateFromCurrentVelocity();
            soundManager.RetriggerBuffer(ID, rate);
        }else if(collisionInfo.gameObject.CompareTag("Horizontal_Wall")){
            
        }
    }
}
