﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmotionRegulation.Components;
using EmotionalAppraisal.DTOs;
using WellFormedNames;
using IntegratedAuthoringTool;
using GAIPS.Rage;
using EmotionRegulation.BigFiveModel;
using RolePlayCharacter;
using System.Diagnostics;
using WorldModel;
using ActionLibrary;
using AutobiographicMemory.DTOs;
using ActionLibrary.DTOs;

namespace Test.EmotionalRegulation
{
    [TestClass]
    public class ERTest
    {
        RolePlayCharacterAsset Pedro;
        RolePlayCharacterAsset player;
        List<RolePlayCharacterAsset> _rpcList;
        WorldModelAsset _worldModel;
        IntegratedAuthoringToolAsset _iat;
        bool initialized = false;
        string currentState;
        string nextState;
        BaseAgent agent;


        [TestMethod]
        public void TestSimulations()
        {
            /// Load the path of the files FAtiMA
            var pathRoot = @"D:\RepoUnity\FAtiMA-Starter-Kit-01\Assets\StreamingAssets\SingleCharacter\";
            var pathScenarioSimple = pathRoot + "scenario4.json";
            var pathStorageSimple = pathRoot + "storage4.json";
            ///Load the scenario
            var storageSimple = AssetStorage.FromJson(File.ReadAllText(pathStorageSimple));
            _iat = IntegratedAuthoringToolAsset.FromJson(File.ReadAllText(pathScenarioSimple), storageSimple);
            _worldModel = _iat.WorldModel;

            _rpcList = _iat.Characters.ToList();
            //ChooseCharacter and load game.
            Pedro = _rpcList.First(a => a.CharacterName.ToString() == "Pedro");
            player = _rpcList.First(a1 => a1.CharacterName.ToString() == "Player");
            //assumption: de user have chosen one
            //MOdificación 1.
            LoadGame(player);
            do
            {
                if (initialized)
                {
                    IAction finalDecision = null;
                    String initiatorAgent = "";
                    IAction newDecision = null;
                    foreach (var rpc in _rpcList)
                    {

                        // From all the decisions the rpc wants to perform we want the first one (as it is ordered by priority)
                        var decision = rpc.Decide().FirstOrDefault();
                        if (decision is null) goto GO;
                        //if (decision.Name.GetNTerm(4) == (Name)"GoToNeutral")
                        //return;
                        GO: /// Paso 1.- Mandamos la posible decisión del agente a la arquitectura de regulación emocional
                        /// para generar una nueva decisión de ser necesario.
                        if (agent.AgentName == rpc.CharacterName) { newDecision = agent.Regulates(decision); }

                        if (!(newDecision is null)) { decision = newDecision; }
                        ///the mood calculate seem bad

                        if (player.CharacterName == rpc.CharacterName)
                        {
                            HandlePlayerOptions(decision);
                        }

                        if (decision != null)
                        {
                            initiatorAgent = rpc.CharacterName.ToString();
                            finalDecision = decision;

                            //Write the decision
                            Debug.Print(" " + initiatorAgent + " decided to " + decision.Name.ToString() + " towards " + decision.Target);

                            break;
                        }

                    }

                    if (finalDecision != null)

                    {
                        if (finalDecision.Key == (Name)"Speak")
                            ChooseDialogue(finalDecision, (Name)initiatorAgent);
                        else
                            Effects(finalDecision, (Name)initiatorAgent);
                    }

                }
                Debug.Print($"Nex state: {nextState}");
            } while (nextState != "End");
        }
        private void Effects(IAction finalAction, Name initiator)
        {

            var eventName = EventHelper.ActionEnd(initiator, finalAction.Name, finalAction.Target);

            //Inform each participating agent of what happened

            _rpcList.Find(x => x.CharacterName == initiator).Perceive(eventName);
            _rpcList.Find(x => x.CharacterName == finalAction.Target).Perceive(eventName);
            //Handle the consequences of their actions
            HandleEffects(eventName);

        }

        private void LoadGame(RolePlayCharacterAsset rpc)
        {

            player = rpc;
            player.IsPlayer = true;

            // Checking information about the characters on scene.
            Debug.Print("Player : " + player.CharacterName);
            var CharacterPlay = player.CharacterName;
            var characterOnScene = _rpcList.FirstOrDefault(a => a.CharacterName != CharacterPlay);
            Debug.Print("Character : " + characterOnScene.CharacterName);

            SetEmotionRegualtionData(_rpcList.Find(c => c.CharacterName != rpc.CharacterName));

            initialized = true;

        }
        private void HandlePlayerOptions(IAction decision)
        {
            if (decision != null)
                if (decision.Key.ToString() == "Speak")
                {
                    //                                          NTerm: 0     1     2     3     4
                    // If it is a speaking action it is composed by Speak ( [ms], [ns] , [m}, [sty])
                    currentState = decision.Name.GetNTerm(1).ToString();
                    nextState = decision.Name.GetNTerm(2).ToString();
                    var meaning = decision.Name.GetNTerm(3);
                    var style = decision.Name.GetNTerm(4);


                    // Returns a list of all the dialogues given the parameters
                    var dialog = _iat.GetDialogueActions((Name)currentState, (Name)"*", (Name)"*", (Name)"*");

                    foreach (var d in dialog)
                    {
                        d.Utterance = player.ProcessWithBeliefs(d.Utterance);
                        Debug.Print("The player performance : " + d.Utterance.ToString() + " To " + decision.Target);//Qué se obtiene como salida?
                    }
                }

                else Debug.Print("Unknown action: " + decision.Key);

        }
        private void ChooseDialogue(IAction action, Name initiator)
        {
            Debug.Print(" The agent " + initiator + " decided to perform " + action.Name + " towards " + action.Target);

            //                                          NTerm: 0     1     2     3     4
            // If it is a speaking action it is composed by Speak ( [ms], [ns] , [m}, [sty])
            currentState = action.Name.GetNTerm(1).ToString();
            nextState = action.Name.GetNTerm(2).ToString();
            var meaning = action.Name.GetNTerm(3);
            var style = action.Name.GetNTerm(4);

            // Returns a list of all the dialogues given the parameters but in this case we only want the first element
            var dialog = _iat.GetDialogueActions((Name)currentState, (Name)nextState, meaning, style).FirstOrDefault();

            if (dialog != null)
                Reply(dialog.Id, initiator, action.Target);
        }
        private void Reply(Guid id, Name initiator, Name target)
        {
            // Retrieving the chosen dialog object
            var dialog = _iat.GetDialogActionById(id);
            var utterance = dialog.Utterance;
            var meaning = dialog.Meaning;
            var style = dialog.Style;
            nextState = dialog.NextState;
            currentState = dialog.CurrentState;

            //Writing the dialog
            Debug.Print("\n" + initiator + " says:  '" + utterance + "' ->towards " + target + "\n");

            // Getting the full action Name
            var actualActionName = "Speak(" + currentState + ", " + nextState + ", " + meaning +
                                   ", " + style + ")";
            //So we generate its event
            var eventName = EventHelper.ActionEnd(initiator, (Name)actualActionName, target);

            //Inform each participating agent of what happened

            _rpcList.Find(x => x.CharacterName == initiator).Perceive(eventName);
            _rpcList.Find(x => x.CharacterName == target).Perceive(eventName);

            //Handle the consequences of their actions
            HandleEffects(eventName);
        }
        private void HandleEffects(Name _event)
        {
            var consequences = _worldModel.Simulate(new Name[] { _event });

            // For each effect 
            foreach (var eff in consequences)
            {
                Debug.Print("Effect: " + eff.PropertyName + " " + eff.NewValue + " " + eff.ObserverAgent);

                // For each Role Play Character
                foreach (var rpc in _rpcList)
                {

                    //If the "Observer" part of the effect corresponds to the name of the agent or if it is a universal symbol
                    if (eff.ObserverAgent != rpc.CharacterName && eff.ObserverAgent != (Name)"*") continue;
                    //Apply that consequence to the agent
                    rpc.Perceive(EventHelper.PropertyChange(eff.PropertyName, eff.NewValue, rpc.CharacterName));

                }
            }
        }
        private void SetEmotionRegualtionData(RolePlayCharacterAsset character)
        {
            /// the first thing that we need to do is provide the data needed for each strategy.
            /// For instance, for the first strategy, wich is situation selection, the information required is all events
            /// that the agent will be able to avoid. All inputs are storagened in the class named RequiredData. As well as 
            /// is necessary set a personality to the character of FAtiMA, it is make with the class named PersonalityDTO.

            PersonalityDTO personalityDTO = new PersonalityDTO() { Conscientiousness = 100 };

            var emotionalAppraisalCharacter = character.m_emotionalAppraisalAsset.GetAllAppraisalRules().ToList();
            List<AppraisalRuleDTO> appRulesOfEvtToAvoid = new List<AppraisalRuleDTO>();
            ///Unfold the events into the Event Matching Template
            emotionalAppraisalCharacter.ForEach(ea => Debug.Print(ea.EventMatchingTemplate.ToString()));
            /// shows the Events Matching Template
            foreach (var appRule in emotionalAppraisalCharacter)
            {
                var eventMatchingTemplate = appRule.EventMatchingTemplate;
                /// checks if the actions rule is a dialigue or not
                var actionName = eventMatchingTemplate.GetNTerm(3);

                if (actionName.GetFirstTerm() == (Name)"Speak")
                {
                    var Style = actionName.GetNTerm(4);
                    /// assumption: the user selec the sad rule.
                    if (Style.Equals((Name)"GoToSad"))
                    {

                        appRulesOfEvtToAvoid.Add(appRule);
                    }

                }
                else if (actionName.Equals((Name)"RunAway"))
                {
                    appRulesOfEvtToAvoid.Add(appRule);
                }

            }

            ///Set news actions for each event 
            /// Unfold all the events that exists into the Event Matching Template

            Debug.Print("EVENTS FOR ACTIONS :");
            emotionalAppraisalCharacter.ForEach(ea => Debug.Print(ea.EventMatchingTemplate.ToString()));

            ActionsforEvent actionsfor = new ActionsforEvent
            {
                NameEventToReact = emotionalAppraisalCharacter.Find(x => x.EventMatchingTemplate.GetNTerm(3) == (Name)"RunAway"),
                ActionsForEventER = new List<KeyValuePair<string, float>>() 
                { 
                    new KeyValuePair<string, float>("Stop",1f),
                    new KeyValuePair<string, float>("RunFaster", -2f),
                }
            };
 
            RequiredData inputs = new RequiredData {  ActionsForEvent = actionsfor, IAT_FAtiMA = _iat  };
            ///The last thing is create the new agent with the personlity, for that, the class named BasedAgent linked the agent 
            ///create on FAtiMA, we need pass the character of FAtiMA, the new personality and the inputs. The next code shows that.
            ///
            agent = new BaseAgent(character, personalityDTO, inputs);
            
        }
    }
}
