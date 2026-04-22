using UnityEngine;
using System.Collections.Generic;

public class song : MonoBehaviour
{
    [Header("=== Playlist Settings ===")]
    [Tooltip("ใส่เพลง/เสียงหลายๆ ไฟล์ตรงนี้ (คลิก + หรือกดลากใส่) เพื่อให้เล่นต่อกัน")]
    public List<AudioClip> playlist = new List<AudioClip>();
    
    [Tooltip("AudioSource สำหรับเล่นเสียง (ถ้าไม่ใส่จะสร้างให้อัตโนมัติ)")]
    public AudioSource audioSource;

    [Tooltip("สุ่มเล่นเพลงหรือไม่?")]
    public bool shuffle = false;

    [Tooltip("ระดับความดังของเสียง (0.0 ถึง 1.0)")]
    [Range(0f, 1f)]
    public float volume = 1f;

    private int currentSongIndex = 0;

    void Start()
    {
        // ค้นหาหรือสร้าง AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.volume = volume;
        // ปิด loop ของ AudioSource เพราะเราจะจัดการเปลี่ยนเพลงเอง
        audioSource.loop = false; 

        if (playlist.Count > 0)
        {
            if (shuffle)
            {
                ShufflePlaylist();
            }
            PlayCurrentSong();
        }
        else
        {
            Debug.LogWarning("[Playlist] ยังไม่ได้ใส่เพลงใน Playlist ของ: " + gameObject.name);
        }
    }

    void Update()
    {
        // ถ้ามีเพลงและ AudioSource ไม่ได้เล่นอยู่ (คือเพลงจบแล้ว) ให้เล่นเพลงถัดไป
        if (playlist.Count > 0 && !audioSource.isPlaying)
        {
            PlayNextSong();
        }
    }

    private void PlayCurrentSong()
    {
        if (playlist.Count == 0 || playlist[currentSongIndex] == null) return;

        audioSource.clip = playlist[currentSongIndex];
        audioSource.Play();
    }

    private void PlayNextSong()
    {
        currentSongIndex++;

        // ถ้าเล่นครบทุกเพลงแล้ว ให้วนกลับไปเพลงแรกสุด
        if (currentSongIndex >= playlist.Count)
        {
            currentSongIndex = 0;
            // ถ้าต้องการให้สุ่มใหม่ทุกรอบที่วนซ้ำ สามารถเอา comment ด้านล่างออกได้
            // if (shuffle) ShufflePlaylist();
        }

        PlayCurrentSong();
    }

    // ฟังก์ชันสำหรับสับเปลี่ยนลำดับเพลงแบบสุ่ม
    private void ShufflePlaylist()
    {
        for (int i = 0; i < playlist.Count; i++)
        {
            AudioClip temp = playlist[i];
            int randomIndex = Random.Range(i, playlist.Count);
            playlist[i] = playlist[randomIndex];
            playlist[randomIndex] = temp;
        }
    }
}
