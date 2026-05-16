using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AudioInfo
{
    public string audioName;
    public List<AudioClip> audioClip;
}
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public AudioInfo[] audioInfos;
    public Dictionary<string, List<AudioClip>> allSoundsDict = new Dictionary<string, List<AudioClip>>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
        DontDestroyOnLoad(this.gameObject);
        for (int i = 0; i < audioInfos.Length; i++)
        {
            if (!allSoundsDict.ContainsKey(audioInfos[i].audioName))
            {
                allSoundsDict.Add(audioInfos[i].audioName, audioInfos[i].audioClip);
            }
        }
        //for (int i = 0; i < GameObject.Find("bgm").transform.childCount; i++)
        //{
        //    StopBGM(GameObject.Find("bgm").transform.GetChild(i).name);
        //}
    }

    public void StopAllBGM()
    {
        for (int i = 0; i < GameObject.Find("bgm").transform.childCount; i++)
        {
            StopBGM(GameObject.Find("bgm").transform.GetChild(i).name);
        }
    }
    public void PlaySounds(string name,Vector2 pos)
    {
        if (allSoundsDict.ContainsKey(name))
        {
            AudioSource.PlayClipAtPoint(allSoundsDict[name][Random.Range(0, allSoundsDict[name].Count)], pos);
        }
    }

    public void PlaySound(string name,int soundId)
    {
        if (allSoundsDict.ContainsKey(name))
        {
            GetComponent<AudioSource>().PlayOneShot(allSoundsDict[name][soundId]);
        }
    }
    public void PlaySounds(string name)
    {
        if (allSoundsDict.ContainsKey(name))
        {
            GetComponent<AudioSource>().PlayOneShot(allSoundsDict[name][Random.Range(0, allSoundsDict[name].Count)]);
        }
    }

    public void StopSounds(string name)
    {
        if (allSoundsDict.ContainsKey(name))
        {
            if (GetComponent<AudioSource>().isPlaying)
            {
                GetComponent<AudioSource>().Stop();
            }
        }
    }
    public void ResumeBGM(string name)
    {
        GameObject.Find(name).GetComponent<AudioSource>().UnPause();
    }
    public void PauseBGM(string name)
    {
        GameObject.Find(name).GetComponent<AudioSource>().Pause();
    }
    public void SetBGMVolume(string name ,float volume)
    {
        GameObject.Find(name).GetComponent<AudioSource>().volume = volume;
    }
    public AudioSource PlayBGM(string name, ulong delay)
    {
        AudioSource aus= GameObject.Find(name).GetComponent<AudioSource>();
        aus.pitch += Random.Range(-0.05f, 0.05f);
        if (!aus.isPlaying)
        {
            aus.Play(delay);
        }
        return aus;
    }
    public AudioSource PlayBGM(string name)
    {
        AudioSource aus = GameObject.Find(name).GetComponent<AudioSource>();
        aus.pitch += Random.Range(-0.05f, 0.05f);
        if (!aus.isPlaying)
        {
            aus.Play();
        }
        return aus;
    }
    public void StopBGM(string name) {
        GameObject.Find(name).GetComponent<AudioSource>().Stop();
    }

}
