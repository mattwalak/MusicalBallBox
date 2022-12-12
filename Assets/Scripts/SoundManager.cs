using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public const int NUM_VOICES = 100;

    private bool[] takenIDs;
    private ChuckSubInstance chuckSubInstance;

    private double sampleLength;
    private Chuck.FloatCallback sampleLengthCallback;

    // Start is called before the first frame update
    void Start()
    {
        chuckSubInstance = GetComponent<ChuckSubInstance>();

        chuckSubInstance.RunCode(@"
        global Event updateSample;
        global Event killAll;

        ""Dialup.wav"" => global string filename;

        100 => global int numBuffers;
        global float sampleLength;

        // All of these are 0 to 1
        global float bufferRates[numBuffers];
        global float bufferVolumes[numBuffers];
        global float bufferFilterMod[numBuffers];

        // 0 to 1, used to initiate starting sample in buffer
        // negative value indicates no action needed
        global float initialPositions[numBuffers];

        // false = 0, true = 1
        global int bufferActiveFlags[numBuffers];

        SndBuf buffers[numBuffers];
        LPF filters[numBuffers];

        20000 => global float MAX_LPF;
        100 => global float MIN_LPF;

        -60 => float minGain;
        0 => float maxGain;

        INITIAL_SETUP();
        LOAD_SAMPLES();

        fun float GetGainFromFloat(float val){
            Std.rmstodb(val) => float dB;
            Std.dbtopow(dB) => float power;
            return power;
        }

        // SETUP

        fun void INITIAL_SETUP(){
            for(0 => int i; i < numBuffers; i++){
                0 => buffers[i].gain;
                0 => bufferVolumes[i];
                MAX_LPF => filters[i].freq;
                1 => bufferFilterMod[i];
                1 => filters[i].Q;
                0.25 => filters[i].gain;
                buffers[i] => filters[i] => dac;
                
                1 => bufferRates[i];
                0 => bufferActiveFlags[i];
                -1 => initialPositions[i];
            }
        }

        fun void LOAD_SAMPLES(){
            me.dir() + filename => string samplePath;
            for(0 => int i; i < numBuffers; i++){
                samplePath => buffers[i].read;
            }
            buffers[0].length()/second => sampleLength;
        }

        fun void KILL_ALL(){
            for(0 => int i; i < numBuffers; i++){
                0 => buffers[i].gain;
            }
        }

        fun void waitForUpdateSample(){
            while(true){
                updateSample => now;
                LOAD_SAMPLES();
            }
        }

        fun void waitForKillAll(){
            while(true){
                killAll => now;
                KILL_ALL();
            }
        }

        spork ~ waitForUpdateSample();

        // Infinite loop
        while(true){
            0 => int activeBufferCount;
            for(0 => int i; i < numBuffers; i++){
                if(bufferActiveFlags[i] != 0){
                    activeBufferCount++;
                }
            }
            
            1.0 / activeBufferCount => float maxGain;

            for(0 => int i; i < numBuffers; i++){
                if(initialPositions[i] >= 0){
                    (initialPositions[i] * buffers[i].samples()) $ int => int pos;
                    pos => buffers[i].pos;
                    
                    bufferRates[i] => buffers[i].rate;
                    
                    1 => buffers[i].gain;
                    -1 => initialPositions[i];
                }
            }
            
            
            for(0 => int i; i < numBuffers; i++){
                if(bufferActiveFlags[i] != 0){
                    // Check for rate change
                    if(bufferRates[i] != buffers[i].rate()){
                        if(bufferRates[i] < 0){
                            // Play in reverse
                            buffers[i].samples() => buffers[i].pos;
                            bufferRates[i] => buffers[i].rate;
                        }else{
                            // Play forward
                            0 => buffers[i].pos;
                            bufferRates[i] => buffers[i].rate;
                        }
                    }
                    
                    // Update all modulated variables
                    GetGainFromFloat(bufferVolumes[i]) * maxGain => buffers[i].gain;
                    (GetGainFromFloat(bufferFilterMod[i]) * (MAX_LPF - MIN_LPF)) + MIN_LPF => filters[i].freq;
                }
            }

            5::ms => now;
        }
	    ");

        takenIDs = new bool[NUM_VOICES];
        for(int i = 0; i < NUM_VOICES; i++){
            takenIDs[i] = false;
        }

        sampleLengthCallback = chuckSubInstance.CreateGetFloatCallback(GetSampleLengthCallback);
        chuckSubInstance.GetFloat("sampleLength", sampleLengthCallback);
    }

    public void ReloadSample(string filename){
        Debug.Log("Reloading sample: " + filename);
        chuckSubInstance.SetString("filename", filename);
        chuckSubInstance.BroadcastEvent("updateSample");
    }

    private void GetSampleLengthCallback(double length){
        sampleLength = length;
        Debug.Log("Sample length = " + sampleLength);
    }

    // Update is called once per frame
    void Update()
    {
        if(sampleLength <= 0){
            chuckSubInstance.GetFloat("sampleLength", sampleLengthCallback);
        }
    }

    public int OpenNewVoice(float normX, float normY, double rate, float normEnergy){
        for(int i = 0; i < NUM_VOICES; i++){
            if(!takenIDs[i]){
                takenIDs[i] = true;
                
                // Set initializing values
                chuckSubInstance.SetFloatArrayValue("bufferRates", (uint) i, rate);
                chuckSubInstance.SetFloatArrayValue("bufferVolumes", (uint) i, normEnergy);
                chuckSubInstance.SetFloatArrayValue("bufferFilterMod", (uint) i, normY);
                chuckSubInstance.SetFloatArrayValue("initialPositions", (uint) i, normX);
                chuckSubInstance.SetIntArrayValue("bufferActiveFlags", (uint) i, 1);

                return i;
            }
        }

        Debug.Log("ERROR - No open voices");
        return 0;
    }

    public void FreeVoice(int id){
        takenIDs[id] = false;
        chuckSubInstance.SetFloatArrayValue("bufferActiveFlags", (uint) id, 0);
        chuckSubInstance.SetFloatArrayValue("initialPositions", (uint) id, -1);
        chuckSubInstance.SetFloatArrayValue("bufferVolumes", (uint) id, 0);

    }

    public void UpdateModulatedData(int ID, float normX, float normY, float normEnergy){
        chuckSubInstance.SetFloatArrayValue("bufferVolumes", (uint) ID, normEnergy);
        chuckSubInstance.SetFloatArrayValue("bufferFilterMod", (uint) ID, normY);
        // chuckSubInstance.SetFloatArrayValue("initialPositions", (uint) ID, normX);
    }

    public void RetriggerBuffer(int ID, double rate){
        chuckSubInstance.SetFloatArrayValue("bufferRates", (uint) ID, rate);
    }

    public double GetSampleLength(){
        return sampleLength;
    }

    public void KillAll(){
        chuckSubInstance.BroadcastEvent("killAll");
    }
}
