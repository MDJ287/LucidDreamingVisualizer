using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace LucidDreamingVisualizer
{
    public class LucidDreamingVisualizer : ModBehaviour
    {
        public static LucidDreamingVisualizer Instance;

        public OWCamera owCamera;
        GameObject promptCanvas;
        RawImage img;
        Image rearviewReticle;
        bool isRearViewToggled = false;

        KeyControl rearViewKey;
        KeyControl loadDreamKey;
        KeyControl stop0gKey;
        KeyControl fartherKey;
        KeyControl nearerKey;
        KeyControl timesTenModifierKey;
        bool rearViewToggle;
        bool mirrorRearViewCamera;

        int rearViewCameraX;
        int rearViewCameraY;
        int rearViewCameraWidth;
        int rearViewCameraHeight;

        float distanceFromShip = 600;

        public void Awake()
        {
            Instance = this;
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
        }

        public void Start()
        {
            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"My mod {nameof(LucidDreamingVisualizer)} is loaded!", MessageType.Success);

            new Harmony("MDJ287.LucidDreamingVisualizer").PatchAll(Assembly.GetExecutingAssembly());

            // Example of accessing game code.
            OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen); // We start on title screen
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        }

        public override void Configure(IModConfig config)
        {
            rearViewKey = Keyboard.current.FindKeyOnCurrentKeyboardLayout(config.GetSettingsValue<string>("rearViewKey"));
            loadDreamKey = Keyboard.current.FindKeyOnCurrentKeyboardLayout(config.GetSettingsValue<string>("loadDreamKey"));
            stop0gKey = Keyboard.current.FindKeyOnCurrentKeyboardLayout(config.GetSettingsValue<string>("stop0gKey"));
            fartherKey = Keyboard.current.FindKeyOnCurrentKeyboardLayout(config.GetSettingsValue<string>("fartherKey"));
            nearerKey = Keyboard.current.FindKeyOnCurrentKeyboardLayout(config.GetSettingsValue<string>("nearerKey"));
            timesTenModifierKey = Keyboard.current.FindKeyOnCurrentKeyboardLayout(config.GetSettingsValue<string>("timesTenModifierKey"));
            rearViewToggle = config.GetSettingsValue<bool>("rearViewToggle");
            mirrorRearViewCamera = config.GetSettingsValue<bool>("mirrorRearViewCamera");

            rearViewCameraX = config.GetSettingsValue<int>("rearViewCameraX");
            rearViewCameraY = config.GetSettingsValue<int>("rearViewCameraY");
            rearViewCameraWidth = config.GetSettingsValue<int>("rearViewCameraWidth");
            rearViewCameraHeight = config.GetSettingsValue<int>("rearViewCameraHeight");

            if (!rearViewToggle) isRearViewToggled = false;

            Destroy(img);
            Destroy(rearviewReticle);

            if (isRearViewToggled)
            {
                CreateRearViewMirror();
            }
        }

        public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
        {
            switch (newScene)
            {
                case OWScene.SolarSystem:
                case OWScene.EyeOfTheUniverse:
                    promptCanvas = GameObject.Find("ScreenPromptCanvas");
                    break;
            }
        }

        private void CreateRearViewMirror()
        {
            owCamera.aspect = rearViewCameraWidth / rearViewCameraHeight;
            owCamera.targetTexture = new RenderTexture(rearViewCameraWidth, rearViewCameraHeight, 16);

            owCamera.targetTexture.Create();
            GameObject imgGameObj = new();
            imgGameObj.transform.SetParent(promptCanvas.transform);
            img = imgGameObj.AddComponent<RawImage>();
            img.texture = owCamera.targetTexture;
            owCamera.targetTexture.Release();
            // most of this probably doesn't do anything
            img.rectTransform.anchorMin = new(0, 0);
            img.rectTransform.anchorMax = new(0, 0);
            img.rectTransform.pivot = new(0, 0);
            img.rectTransform.offsetMin = new(0, 0);
            img.rectTransform.offsetMax = new(rearViewCameraWidth, rearViewCameraHeight);
            img.transform.position = new(rearViewCameraX, rearViewCameraY, 0);
            if (mirrorRearViewCamera)
            {
                img.transform.position += new Vector3(rearViewCameraWidth, 0, 0);
                img.transform.localScale = new(-img.transform.localScale.x, img.transform.localScale.y, img.transform.localScale.z);
            }

            GameObject rearviewReticleGameObj = new();
            rearviewReticleGameObj.transform.SetParent(promptCanvas.transform);
            rearviewReticle = rearviewReticleGameObj.AddComponent<Image>();
            rearviewReticle.sprite = FindObjectOfType<ReticleController>()._defaultReticle;
            rearviewReticle.rectTransform.anchorMin = new(0, 0);
            rearviewReticle.rectTransform.anchorMax = new(0, 0);
            rearviewReticle.rectTransform.pivot = new(0, 0);
            rearviewReticle.rectTransform.offsetMin = new(0, 0);
            rearviewReticle.rectTransform.offsetMax = new(16, 16);
            rearviewReticle.transform.position = new(rearViewCameraX + rearViewCameraWidth / 2 - 8, rearViewCameraY + rearViewCameraHeight / 2 - 8);
        }

        public void Update()
        {
            /*
             * LOOK AT:
             * PlayerCameraController
            */
            // set from player settings?
            if (Locator._playerCamera == null || Locator._playerBody == null)
            {
                return;
            }

            if (owCamera == null)
            {
                GameObject cameraGameObj = new();
                cameraGameObj.transform.SetParent(Locator._playerBody.transform);
                owCamera = cameraGameObj.AddComponent<OWCamera>();
                owCamera.mainCamera.CopyFrom(Locator._playerCamera.mainCamera);
                owCamera.aspect = rearViewCameraWidth / rearViewCameraHeight;
                owCamera.targetTexture = new RenderTexture(rearViewCameraWidth, rearViewCameraHeight, 16);
                if (isRearViewToggled)
                {
                    CreateRearViewMirror();
                }
            }

            if ((!rearViewToggle && rearViewKey.isPressed) || isRearViewToggled)
            {
                owCamera.transform.SetPositionAndRotation(Locator._shipBody.transform.position, Locator._shipBody.transform.rotation);
                owCamera.transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(owCamera.transform.forward, Locator._sunTransform.position - owCamera.transform.position, 7, 0));
                owCamera.transform.position += owCamera.transform.forward * distanceFromShip;
            }
            if (rearViewKey.wasPressedThisFrame)
            {
                if (rearViewToggle)
                {
                    isRearViewToggled = !isRearViewToggled;
                }
                if (!rearViewToggle || isRearViewToggled)
                {
                    CreateRearViewMirror();
                }
            }
            if (!rearViewToggle ? rearViewKey.wasReleasedThisFrame : (!isRearViewToggled && rearViewKey.wasPressedThisFrame))
            {
                Destroy(img);
                Destroy(rearviewReticle);
            }

            if (loadDreamKey.wasPressedThisFrame)
            {
                Locator._dreamWorldController._dreamWorldVolume.AddObjectToVolume(Locator.GetPlayerDetector());
                DreamArrivalPoint.Location[] locations = {
                    DreamArrivalPoint.Location.Zone1,
                    DreamArrivalPoint.Location.Zone2,
                    DreamArrivalPoint.Location.Zone3,
                    DreamArrivalPoint.Location.Zone4
                };
                foreach (DreamArrivalPoint.Location location in locations)
                {
                    DreamArrivalPoint arrivalPoint = Locator.GetDreamArrivalPoint(location);
                    Sector sector = arrivalPoint.GetSector();
                    while (sector != null)
                    {
                        sector.GetTriggerVolume().AddObjectToVolume(Locator.GetProbe().GetSectorDetector().gameObject);
                        sector = sector.GetParentSector();
                    }
                }
            }
            if (stop0gKey.wasPressedThisFrame) {
                Locator.GetRingWorldController().GetRingWorldBody().GetComponentInChildren<ZeroGVolume>().GetOWTriggerVolume().RemoveObjectFromVolume(Locator.GetShipDetector());
            }
            if (fartherKey.wasPressedThisFrame)
            {
                distanceFromShip += timesTenModifierKey.isPressed ? 10 : 1;
            }
            if (nearerKey.wasPressedThisFrame)
            {
                distanceFromShip -= timesTenModifierKey.isPressed ? 10 : 1;
            }
        }
    }

}
