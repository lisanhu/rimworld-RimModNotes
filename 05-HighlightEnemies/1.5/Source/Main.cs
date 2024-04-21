﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using Verse.Noise;
using Verse.Grammar;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Net;
using HugsLib.Utils;

// using System.Reflection;
// using HarmonyLib;

namespace _HightlightEnemies
{
    public class EnemyHighlighter : GameComponent
    {
        public bool markEnemies = true;

        private bool MarkInFog => HE_ModSettings.markEnemiesInFog;

        private bool MarkInvisible => HE_ModSettings.markEnemiesInvisible;

        private Task lastTask = Task.CompletedTask;

        private DesignationDef desDef = DefDatabase<DesignationDef>.GetNamed("HE_Mark");

        private HashSet<Thing> previousStatus = new HashSet<Thing>();

        public EnemyHighlighter(Game game) { }

        public override void FinalizeInit()
        {
            markEnemies = HE_ModSettings.markEnemiesByDefault;
        }

        public void ResetStatus()
        {
            previousStatus.Clear();
        }

        public override void GameComponentTick()
        {
            if (markEnemies)
            {
                Highlight();
            }
            else
            {
                DeHighlight();
            }
        }

        private void AddTo(Thing thing, HashSet<Thing> list)
        {
            if (thing != null && !list.Contains(thing))
            {
                if (!MarkInvisible && thing is Pawn p && p.IsPsychologicallyInvisible())
                {
                    return;
                }

                if (!MarkInFog && thing.Fogged())
                {
                    return;
                }
                Log.Message($"Add {thing.Label}");
                list.Add(thing);
            }
        }


        public void Highlight()
        {
            var manager = Find.CurrentMap.designationManager;

            if (lastTask.IsCompleted)
            {
                lastTask = Task.Run(() =>
                {
                    var things = Find.CurrentMap.attackTargetsCache.TargetsHostileToColony.Select(t => t.Thing).ToList();
                    var hostileThings = new HashSet<Thing>();
                    foreach (var thing in things)
                    {
                        if (GenHostility.HostileTo(thing, Faction.OfPlayer))
                        {
                            // hostileThings.Add(thing);
                            AddTo(thing, hostileThings);
                        }
                    }

                    var buildings = Find.CurrentMap.listerBuildings.allBuildingsNonColonist.Where(t => t is Building && t.def.building.combatPower > 0).ToList();
                    foreach (var thing in buildings)
                    {

                        if (GenHostility.HostileTo(thing, Faction.OfPlayer))
                        {
                            // hostileThings.Add(thing);
                            AddTo(thing, hostileThings);
                        }
                    }
                    return hostileThings;
                }).ContinueWith((hostileThings) =>
                {
                    var shouldMarked = hostileThings.Result;
                    
                    var thingsToRemove = previousStatus.Except(shouldMarked);
                    var thingsToAdd = shouldMarked.Except(previousStatus);
                    foreach (var thing in thingsToRemove)
                    {
                        manager.RemoveDesignation(manager.DesignationOn(thing, desDef));
                    }

                    foreach (var thing in shouldMarked.Except(previousStatus))
                    {
                        manager.AddDesignation(new Designation(thing, desDef));
                    }
                    previousStatus = shouldMarked;
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        public void DeHighlight()
        {
            var manager = Find.CurrentMap.designationManager;
            manager.RemoveAllDesignationsOfDef(desDef);
        }

        [StaticConstructorOnStartup]
        public static class Start
        {
            static Start()
            {
                Log.Message("HightLightEnemies loaded");
                var harmony = new Harmony("com.RunningBugs.AlertUtility");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
        }

        [HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls", MethodType.Normal)]
        public class ToggleIconPatcher
        {
            [HarmonyPostfix]
            public static void AddIcon(WidgetRow row, bool worldView)
            {
                if (worldView) return;

                var eh = Current.Game.GetComponent<EnemyHighlighter>();
                if (eh != null)
                {
                    var before = eh.markEnemies;
                    row.ToggleableIcon(ref eh.markEnemies, ContentFinder<Texture2D>.Get("HE/UI/Alarm", true), "HighlightEnemies".Translate(), SoundDefOf.Mouseover_ButtonToggle, (string)null);
                    if (before != eh.markEnemies)
                    {
                        eh.ResetStatus();
                    }
                }
            }
        }

    }
}