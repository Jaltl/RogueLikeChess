using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;

public class AnimationController : MonoBehaviour
{
    Queue<IEnumerator> queue = new();
    bool animating = false;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip captureSound;
    [SerializeField] private AudioClip selectSound;

    public bool IsAnimating() => animating;

    public void Play(IEnumerator anim)
    {
        queue.Enqueue(anim);
        if (!animating)
            StartCoroutine(Process());
    }

    IEnumerator Process()
    {
        animating = true;

        while (queue.Count > 0)
        {
            yield return StartCoroutine(queue.Dequeue());
        }

        animating = false;
    }

    // MOVE PIECE
    public IEnumerator Move(Piece piece, Vector3 target)
    {
        piece.transform.localScale = Vector3.one;
        float duration = 0.3f;
        float t = 0;

        Vector3 start = piece.transform.position;

        // ANTICIPATION (tiny backward move)
        Vector3 dir = (target - start).normalized;
        piece.transform.position = start - dir * 0.1f;

        yield return new WaitForSeconds(0.05f);

        while (t < duration)
        {
            float p = t / duration;

            // ease in-out cubic
            p = p * p * (3f - 2f * p);

            // arc
            float height = Mathf.Sin(p * Mathf.PI) * 0.4f;

            piece.transform.position =
                Vector3.Lerp(start, target, p) + Vector3.up * height;

            // squash & stretch
            float scaleY = 1 + height * 0.5f;
            float scaleX = 1 - height * 0.3f;

            piece.transform.localScale = new Vector3(scaleX, scaleY, 1);

            t += Time.deltaTime;
            yield return null;
        }
        //audioSource.PlayOneShot(moveSound);
        piece.transform.position = target;
        piece.transform.localScale = Vector3.one;
    }

    // CAPTURE ENEMY PIECE
    public IEnumerator Capture(Piece piece)
    {
        float t = 0;
        float duration = 0.2f;

        Vector3 startScale = piece.transform.localScale;

        // quick "pop" before dying
        piece.transform.localScale *= 1.2f;

        while (t < duration)
        {
            float p = t / duration;

            piece.transform.localScale =
                Vector3.Lerp(startScale * 1.2f, Vector3.zero, p);

            piece.transform.rotation *= Quaternion.Euler(0, 0, 10f);

            t += Time.deltaTime;
            yield return null;
        }
        //audioSource.PlayOneShot(captureSound);
        CameraShake();
        Destroy(piece.gameObject);
    }


    public IEnumerator SelectPulse(Piece piece)
    {
        float duration = 0.25f;
        float t = 0;

        Vector3 baseScale = Vector3.one;
        piece.transform.localScale = baseScale;

        while (t < duration)
        {
            float p = t / duration;

            float scale = 1 + Mathf.Sin(p * Mathf.PI * 2f) * 0.1f;

            piece.transform.localScale = baseScale * scale;

            t += Time.deltaTime;
            yield return null;
        }

        piece.transform.localScale = baseScale;
    }

    public void PlaySoundSelect()
    {
        audioSource.PlayOneShot(selectSound);
    }

    IEnumerator CameraShake()
    {
        Vector3 originalPos = Camera.main.transform.position;

        float t = 0;
        while (t < 0.15f)
        {
            Camera.main.transform.position = originalPos + UnityEngine.Random.insideUnitSphere * 0.1f;

            t += Time.deltaTime;
            yield return null;
        }
        Camera.main.transform.position = originalPos;
    }
}
