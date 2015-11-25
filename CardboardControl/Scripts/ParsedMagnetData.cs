﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
* Dealing with raw magnet input from a Cardboard device
*/
public class ParsedMagnetData {
  private struct MagnetMoment {
  	public float deltaTime;
  	public float yMagnitude;
    public MagnetMoment(float deltaTime, float yMagnitude) {
      this.deltaTime = deltaTime;
      this.yMagnitude = yMagnitude;
    }
  }
  private struct MagnetWindowState {
    public float firstHalf;
    public float lastHalf;
    public float ratio; // TODO: do I only need this?
  }
  private List<MagnetMoment> magnetWindow;
  private MagnetWindowState currentMagnetWindow;
  private float MAX_WINDOW_SECONDS = 0.2f;
  private float MAGNET_RATIO_THRESHOLD = 0.1f;
  private float CALIBRATION_SECONDS = 1f;
  private float windowLength = 0.0f;

  enum TriggerState {
    Negative,
    Neutral,
    Postive
  };
  private bool wasTriggering = false;
  private TriggerState triggerState = TriggerState.Neutral;
  private bool isDown = false;

  public ParsedMagnetData() {
    Input.compass.enabled = true;
    magnetWindow = new List<MagnetMoment>();
    windowLength = 0.0f;
  }

  public void Update() {
    TrimMagnetWindow();
    AddToMagnetWindow();
    currentMagnetWindow = CaptureMagnetWindow();
    TriggerState newTriggerState = GetTriggerState();
    if (newTriggerState != TriggerState.Neutral && triggerState != newTriggerState) {
      isDown = !isDown;
      triggerState = newTriggerState;
    }
  }

  private TriggerState GetTriggerState() {
    if (Time.time < CALIBRATION_SECONDS) return TriggerState.Neutral;
    if (currentMagnetWindow.ratio < 1f-MAGNET_RATIO_THRESHOLD) return TriggerState.Negative;
    if (currentMagnetWindow.ratio > 1f+MAGNET_RATIO_THRESHOLD) return TriggerState.Postive;
    return TriggerState.Neutral;
  }

  public void TrimMagnetWindow() {
    while (windowLength > MAX_WINDOW_SECONDS) {
      MagnetMoment moment = magnetWindow[0];
      magnetWindow.RemoveAt(0);
      windowLength -= moment.deltaTime;
    }
  }

  public void AddToMagnetWindow() {
    magnetWindow.Add(new MagnetMoment(Time.deltaTime, Input.compass.rawVector.magnitude));
    windowLength += Time.deltaTime;
  }

  private MagnetWindowState CaptureMagnetWindow() {
    MagnetWindowState newState = new MagnetWindowState();
    int middle = magnetWindow.Count / 2;
    List<MagnetMoment> firstHalf = magnetWindow.GetRange(0, middle);
    List<MagnetMoment> lastHalf = magnetWindow.GetRange(middle, magnetWindow.Count - middle);
    newState.firstHalf = Average(firstHalf);
    newState.lastHalf = Average(lastHalf);
    newState.ratio = newState.firstHalf / newState.lastHalf;
    return newState;
  }

  private float Average(List<MagnetMoment> moments) {
    if (moments.Count == 0) return 0.0f;
    float sum = 0.0f;
    for (int index = 0; index < moments.Count; index++) {
      sum += moments[index].yMagnitude;
    }
    return sum / moments.Count;
  }

  private bool IsMagnetGoingDown(float min, float max, float start) {
    float minDelta = Mathf.Abs(min - start);
    float maxDelta = Mathf.Abs(max - start);
    return minDelta > maxDelta;
  }

  public bool IsDown() {
    return isDown;
  }

  public bool IsUp() {
    return !isDown;
  }
}
