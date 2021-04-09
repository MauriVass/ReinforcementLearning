using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

public class Experience
{
	private float[] state;
	private int action;
	private float reward;
	private float[] nextState;
	private bool endState;

	public Experience(float[] state, int action, float reward, float[] nextState, bool endState)
	{
		this.state = state;
		this.action = action;
		this.reward = reward;
		this.nextState = nextState;
		this.endState = endState;
	}

	public float[] State { get => state; set => state = value; }
	public int Action { get => action; set => action = value; }
	public float[] NextState { get => nextState; set => nextState = value; }
	public float Reward { get => reward; set => reward = value; }
	public bool EndState { get => endState; set => endState = value; }

	public string ToString()
	{
		string output = "";

		output += string.Join(".",state) + ",";
		output += action + ",";
		output += string.Join(".", nextState) + ",";
		output += reward + ",";
		output += endState;

		return output;
	}
	public static Experience FromString(string text)
	{
		string[] values = text.Split(',');

		float[] state = Array.ConvertAll(values[0].Split('.'), s => float.Parse(s));
		int action = int.Parse(values[1]);
		float[] nextstate = Array.ConvertAll(values[2].Split('.'), s => float.Parse(s));
		float reward = float.Parse(values[3]);
		bool endstate = bool.Parse(values[4]);
		Experience e = new Experience(state,action,reward,nextstate, endstate);
		return e;
	}
}

public class AgentDQL : MonoBehaviour
{
	public Controller controller;

	//For Deep Q-Learning
	int sizeReplayMemory;
	int indexExperience;
	Experience[] replayMemory;
	int batchReplayMemory;
	List<Experience> batchExperiences;
	int batchSize;
	bool useLocalNN;
	public NeuralNetwork policyNetwork, targetNetwork;
	int counterTargetNet, updateTargetNet;
	float rateAveraging;
	bool startLearning;

	[HideInInspector]
	public int n, m;
	int nInput;

	Client client;

	void Start()
	{
		n = controller.n;
		m = controller.m;

		/*Deep Q-Learning*/
		//This should be a large number ~10^5
		sizeReplayMemory = 2000;
		indexExperience = 0;
		//Inputs are:
		//   adjacent platforms (4 in the case of squares and 6 for the hexagones)
		//   4 binary values to understand the target position wrt the actor position
		nInput = controller.nActions + 4;
		replayMemory = new Experience[sizeReplayMemory];

		//Use local NN (true) or remote one (false)(remember to activate server.py script!)
		useLocalNN = false;
		if (useLocalNN)
		{
			policyNetwork = new NeuralNetwork(nInput, 1, 64, controller.nActions, 0.01f, 0.9f);
			targetNetwork = new NeuralNetwork(policyNetwork);
			//targetNetwork.CopyNN(policyNetwork.hiddenWeights, policyNetwork.outputWeights);
        }
        else
        {
			client = new Client();
        }

		batchExperiences = new List<Experience>();
		batchSize = 64;
		updateTargetNet = 5;
		rateAveraging = 0.01f;
		startLearning = false;

		ReadFromFile();
		if (indexExperience >= sizeReplayMemory)
			startLearning = true;

	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.E))
		{
			SaveToFile();
			Debug.LogError("Wrote on file");
		}
	}
	public Vector2 ChooseAction()
	{
		float n = Random.Range(0f, 1f);
		Vector2 action = new Vector2(-1, -1);
		if (n < controller.epsilon)
		{
			action = controller.isSquare ? ActionSquare.ChooseRandomAction() : ActionHex.ChooseRandomAction((int)(controller.agentPosition.y) % 2);
			if (startLearning)
				controller.epsilon = controller.epsilon > controller.minEplison ? controller.epsilon - controller.epsilonDecay : controller.minEplison;
			print("Random Action Chosen: " + action);
		}
		else
		{
			float maxValue = -999;
			float[] currentInput = controller.getCurrentState(nInput, controller.agentPosition);
			FindMaxValueDQL(currentInput, out action, out maxValue);
			print("Action Chosen: " + action + " maxValue: " + maxValue);
		}

		controller.steps++;
		if (controller.steps > controller.maxSteps)
		{
			controller.end = true;
		}
		return action;
	}

	void FindMaxValueDQL(float[] input, out Vector2 action, out float maxValue)
	{
		maxValue = -999;
		int indexAction = -1;

		//Action future that maximize the reward
		Vector2 maxValueAction = new Vector2(-1, -1);

		float[] output;
		if (useLocalNN)
		{
			///Local Neural Network
			policyNetwork.StepsForward(input);
			output = policyNetwork.getOutput();
		}
		else
		{
			///Remote Neural Network
			output = client.predict(input,true);
		}

		for (int i = 0; i < controller.nActions; i++)
		{
			if (maxValue < output[i])
			{
				maxValue = output[i];
				indexAction = i;
			}
		}

		action = controller.isSquare ? ActionSquare.GetVectorByIndex(indexAction) : ActionHex.GetVectorByIndex(indexAction, (int)(controller.agentPosition.y) % 2);
	}

	public void PerformAction(Vector2 action)
	{
		Vector2 agentPos = controller.agentPosition;
		int indexAction = -1;
		indexAction = controller.isSquare ? ActionSquare.GetIndexByVector(action) : ActionHex.GetIndexByVector(action, (int)(agentPos.y) % 2);
		float reward;
		float maxNextValue;

		float[] currentState = controller.getCurrentState(nInput, controller.agentPosition);
		controller.agentPosition += action;
		float[] nextState = controller.getCurrentState(nInput, controller.agentPosition);

		int x = (int)controller.agentPosition.x;
		int y = (int)controller.agentPosition.y;
		if (controller.checkIndexBoundaries(x, y))
		{
			controller.previousPlatform = controller.currentPlatform;
			controller.currentPlatform = controller.platforms[x, y];

			Vector3 pos = controller.currentPlatform.transform.position + Vector3.back;
			controller.agent.transform.position = pos;

			reward = controller.currentPlatform.reward;

			//The value 'any' is not important
			Vector2 any;
			FindMaxValueDQL(nextState, out any, out maxNextValue);
		}
		else
		{
			//Out of the platform boundaries
			controller.end = true;

			if (controller.DQLearning)
				reward = -1;
			else
				reward = -10;
			maxNextValue = 0;

			print("Out of B");
		}

		controller.end = controller.end ? controller.end : controller.currentPlatform.CheckGameState();

		//Create new Experience
		Experience e = new Experience(currentState, indexAction, reward, nextState, controller.end);
		//Save Experience in the ReplayMemory (also overwritting the existining ones)
		replayMemory[indexExperience % sizeReplayMemory] = e;
		indexExperience++;
		print(e.ToString());
		//Do random actions while the replayMemory is not full
		if (indexExperience >= sizeReplayMemory && !startLearning)
		{
			startLearning = true;
			SaveToFile();
		}
		if (startLearning)
		{
			//Choose a random batch size (avoid takeing the last experiences)
			//batchReplayMemory = Random.Range(sizeReplayMemory/10, indexExperience > sizeReplayMemory ? sizeReplayMemory : indexExperience);
			//Empty batch experiences
			batchExperiences.Clear();
			//Select (different) random Experiences
			while (batchExperiences.Count < batchSize)
			{
				int randExp = Random.Range(0, indexExperience > sizeReplayMemory ? sizeReplayMemory : indexExperience);
				if (!batchExperiences.Contains(replayMemory[randExp]))
					batchExperiences.Add(replayMemory[randExp]);
			}

			float error = 0;
			for (int i = 0; i < batchExperiences.Count; i++)
			{
				if (useLocalNN)
				{
					//Pass the current state through the Policy Network
					currentState = batchExperiences[i].State;
					policyNetwork.StepsForward(currentState);
					//Get the output given the choosen Action
					int currentAction = batchExperiences[i].Action;
					float currentQValue = policyNetwork.GetOutputByActionIndex(currentAction);

					//Pass the next state through the Target Network
					nextState = batchExperiences[i].NextState;
					targetNetwork.StepsForward(nextState);
					//Get the max output
					float nextQValue = targetNetwork.GetMaxOutput();

					//Calculate the loss between the choosen Action and the best one
					float theta = batchExperiences[i].EndState ? batchExperiences[i].Reward : batchExperiences[i].Reward + controller.discountRate * nextQValue;
					float loss = 0.5f * Mathf.Pow(theta - currentQValue, 2) / batchExperiences.Count;
					error += loss;

					//BackPropagate error and update weights
					policyNetwork.StepsBackward(targetNetwork.getOutput());//targetNetwork.getOutput()
				}
				else
				{
					client.fit(batchExperiences[i]);
				}
			}

			//Upadate Target Network weights
			counterTargetNet++;
			if (counterTargetNet > updateTargetNet && startLearning)
			{
				if (useLocalNN)
				{
					//Update Target Net weights with the Policy Net ones
					//Instead of changing each weights and biases
					//It is faster (and the same) to make the 2 Nets equals
					//DO NOT USE
					//targetNetwork = policyNetwork;

					//It seem to give problem with the method above, so use this method
					targetNetwork.CopyNN(policyNetwork.hiddenWeights, policyNetwork.outputWeights);

                }
                else
                {
					client.copyNN();
                }
				counterTargetNet = 0;
			}
		}
	}


	void SaveToFile()
	{
		using (StreamWriter sw = File.CreateText("Assets/Script/PathFinding/Experiences/exp.csv"))
		{
			foreach (Experience e in replayMemory)
			{
				if (e == null)
					break;
				sw.WriteLine(e.ToString());
			}
		}
	}
	void ReadFromFile()
	{
		using (StreamReader sr = File.OpenText("Assets/Script/PathFinding/Experiences/exp.csv"))
		{
			string s;
			while ((s = sr.ReadLine()) != null)
			{
				replayMemory[indexExperience % sizeReplayMemory] = Experience.FromString(s);
				indexExperience++;
			}
		}
	}
}
