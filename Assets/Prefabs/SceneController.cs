using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // required for Coroutines

public enum FadeMode
{
	FadeIn,
	FadeOut,
	FadeInOut,
	FadeOutIn
}

public class SceneController : MonoBehaviour
{
	public int sceneIndex;
	[Tooltip("Fade duration")]
	public float fadeTime = 2.0f;

	[Tooltip("Screen color at maximum fade")]
	public Color fadeColor = new Color(0.01f, 0.01f, 0.01f, 1.0f);

	public bool fadeOnStart = true;

	public FadeMode fadeMode;

	/// <summary>
	/// The render queue used by the fade mesh. Reduce this if you need to render on top of it.
	/// </summary>
	public int renderQueue = 5000;

    public List<OVRScreenFade> fadeScreens;

	private float uiFadeAlpha = 0;

	private MeshRenderer fadeRenderer;
	private MeshFilter fadeMesh;
	private Material fadeMaterial = null;
	private bool isFading = false;

	public float currentAlpha { get; private set; }

	/// <summary>
	/// Automatically starts a fade in
	/// </summary>

	void Start()
	{
		// create the fade material
		fadeMaterial = new Material(Shader.Find("Oculus/Unlit Transparent Color"));
		fadeMesh = gameObject.AddComponent<MeshFilter>();
		fadeRenderer = gameObject.AddComponent<MeshRenderer>();

		var mesh = new Mesh();
		fadeMesh.mesh = mesh;

		Vector3[] vertices = new Vector3[4];

		float width = 2f;
		float height = 2f;
		float depth = 1f;

		vertices[0] = new Vector3(-width, -height, depth);
		vertices[1] = new Vector3(width, -height, depth);
		vertices[2] = new Vector3(-width, height, depth);
		vertices[3] = new Vector3(width, height, depth);

		mesh.vertices = vertices;

		int[] tri = new int[6];

		tri[0] = 0;
		tri[1] = 2;
		tri[2] = 1;

		tri[3] = 2;
		tri[4] = 3;
		tri[5] = 1;

		mesh.triangles = tri;

		Vector3[] normals = new Vector3[4];

		normals[0] = -Vector3.forward;
		normals[1] = -Vector3.forward;
		normals[2] = -Vector3.forward;
		normals[3] = -Vector3.forward;

		mesh.normals = normals;

		Vector2[] uv = new Vector2[4];

		uv[0] = new Vector2(0, 0);
		uv[1] = new Vector2(1, 0);
		uv[2] = new Vector2(0, 1);
		uv[3] = new Vector2(1, 1);

		mesh.uv = uv;

		SetFadeLevel(0);

		if (fadeOnStart)
		{
			StartCoroutine(LoadSceneWithFadeCor(sceneIndex, FadeMode.FadeOut, fadeTime));
		}
	}

    public void ChangeSceneFade()
    {
        StartCoroutine(LoadSceneWithFadeCor(sceneIndex, FadeMode.FadeOut, fadeTime));
    }

    public IEnumerator LoadSceneWithFadeCor(int sceneBuildIndex, FadeMode fadeMode, float fadeTime)
    {
        if (isFading) yield break;

        // Fade Out
        if (fadeMode == FadeMode.FadeOut || fadeMode == FadeMode.FadeOutIn)
        {
            yield return Fade(0, 1);
        }

        if (sceneBuildIndex >= 0)
        {
            var loadAsync = SceneManager.LoadSceneAsync(sceneBuildIndex);
            yield return new WaitUntil(() => loadAsync.isDone);
        }

        if (fadeMode == FadeMode.FadeIn || fadeMode == FadeMode.FadeOutIn)
        {
            yield return Fade(1, 0);
        }
    }


    /// <summary>
    /// Start a fade out
    /// </summary>
    public void FadeOut()
	{
		StartCoroutine(Fade(0, 1));
	}


	/// <summary>
	/// Starts a fade in when a new level is loaded
	/// </summary>
	void OnLevelFinishedLoading(int level)
	{
		StartCoroutine(Fade(1, 0));
	}

	void OnEnable()
	{
		if (!fadeOnStart)
		{
			SetFadeLevel(0);
		}
	}

	/// <summary>
	/// Cleans up the fade material
	/// </summary>
	void OnDestroy()
	{
		if (fadeRenderer != null)
			Destroy(fadeRenderer);

		if (fadeMaterial != null)
			Destroy(fadeMaterial);

		if (fadeMesh != null)
			Destroy(fadeMesh);
	}

	/// <summary>
	/// Set the UI fade level - fade due to UI in foreground
	/// </summary>
	public void SetUIFade(float level)
	{
		uiFadeAlpha = Mathf.Clamp01(level);
		SetMaterialAlpha();
	}
	/// <summary>
	/// Override current fade level
	/// </summary>
	/// <param name="level"></param>
	public void SetFadeLevel(float level)
	{
		currentAlpha = level;
		SetMaterialAlpha();
	}

	/// <summary>
	/// Fades alpha from 1.0 to 0.0
	/// </summary>
	IEnumerator Fade(float startAlpha, float endAlpha)
	{
        foreach( OVRScreenFade fadeScreen in fadeScreens)
        {
            if( fadeMode == FadeMode.FadeOut )
            {
                fadeScreen.FadeOut(fadeTime);
            }
            else if( fadeMode == FadeMode.FadeIn )
            {
                fadeScreen.FadeIn(fadeTime);
            }
        }

		float elapsedTime = 0.0f;
		while (elapsedTime < fadeTime)
		{
			elapsedTime += Time.deltaTime;
			///currentAlpha = Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(elapsedTime / fadeTime));
			///SetMaterialAlpha();
			yield return new WaitForEndOfFrame();
		}
        /*
		FadeCamera fadeCamera = gameObject.GetComponent<FadeCamera>();
		if (fadeCamera != null)
		{
			fadeCamera.FadeOut(fadeTime);
		}
		yield return new WaitForEndOfFrame();
		*/
    }


    /// <summary>
    /// Update material alpha. UI fade and the current fade due to fade in/out animations (or explicit control)
    /// both affect the fade. (The max is taken)
    /// </summary>
    private void SetMaterialAlpha()
	{
		Color color = fadeColor;
		color.a = Mathf.Max(currentAlpha, uiFadeAlpha);
		isFading = color.a > 0;
		if (fadeMaterial != null)
		{
			fadeMaterial.color = color;
			fadeMaterial.renderQueue = renderQueue;
			fadeRenderer.material = fadeMaterial;
			fadeRenderer.enabled = isFading;
		}
	}
}
