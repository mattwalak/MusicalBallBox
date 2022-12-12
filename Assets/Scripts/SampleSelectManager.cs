using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SampleSelectManager : MonoBehaviour
{
    public Button recordButton;
    public TMP_Text recordButtonText;
    public Button playbackButton;
    public SoundManager soundManager;
    public GameManager gameManager;

    public TMP_Dropdown sampleDropdown;

    private ChuckSubInstance chuckSubInstance;
    private bool isRecording = false;
    private bool hasRecorded = false;

    private string[] filenames = new string[]{
        "Dialup.wav", "Bell.wav", "HELL_YEAH.wav", "oooooahhh.wav", 
        "Breakbeat.wav", "River.wav", "custom.wav"};

    private void Start(){
        chuckSubInstance = GetComponent<ChuckSubInstance>();
        chuckSubInstance.RunCode(@"
        global Event startRecording;
        global Event stopRecording;

        0 => int isRecording; // 0 = false, 1 = true;
        -1 => int recID;

        fun void startRecordingMethod(){
            if(isRecording == 0){
                Machine.add(me.dir() + ""rec.ck:"") => recID;
                1 => isRecording;
            }
        }

        fun void stopRecordingMethod(){
            if(isRecording == 1){
                Machine.remove(recID);
                0 => isRecording;
            }
        }

        fun void listenForStartRecording(){
            while(true){
                startRecording => now;
                startRecordingMethod();
            }
        }

        fun void listenForStopRecording(){
            while(true){
                stopRecording => now;
                stopRecordingMethod();
            }
        }

        spork ~ listenForStartRecording();
        spork ~ listenForStopRecording();

        while(true){
            1::second => now;
        }
        ");


    }

    public void OnDropdownChanged(){
        if((sampleDropdown.value == 6) && (isRecording || !hasRecorded)){
            Debug.Log("no custom audio avaliable");
            return;
        }

        gameManager.OnClearBox();
        soundManager.ReloadSample(filenames[sampleDropdown.value]);
    }

    public void OnRecordClicked(){
        if(isRecording){
            isRecording = false;
            playbackButton.interactable = true;
            hasRecorded = true;
            chuckSubInstance.BroadcastEvent("stopRecording");
        }else{
            isRecording = true;
            playbackButton.interactable = false;
            recordButtonText.text = "Stop";
            chuckSubInstance.BroadcastEvent("startRecording");
        }
    }

    public void OnPlaybackClicked(){
        if((sampleDropdown.value == 6) && (isRecording || !hasRecorded)){
            Debug.Log("no custom audio avaliable");
            return;
        }

        string filename = filenames[sampleDropdown.value];

        string code = @"
            me.dir() + """ + filename + @""" => string filename;
            SndBuf buf => dac;

            filename => buf.read;
            1 => buf.gain;
            1 => buf.rate;

            buf.length() => now;";

        chuckSubInstance.RunCode(code);
    }
}
