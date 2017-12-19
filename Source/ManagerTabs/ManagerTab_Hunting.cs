﻿// Karel Kroeze
// ManagerTab_Hunting.cs
// 2016-12-09

using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static FluffyManager.Constants;

namespace FluffyManager
{
    internal class ManagerTab_Hunting : ManagerTab
    {
        private float _leftRowHeight = 9999f;
        private Vector2 _scrollPosition = Vector2.zero;
        private ManagerJob_Hunting _selected;
        private float _topAreaHeight = 30f;

        public List<ManagerJob_Hunting> Jobs;

        public ManagerTab_Hunting( Manager manager ) : base( manager )
        {
            _selected = new ManagerJob_Hunting( manager );
        }

        public override Texture2D Icon
        {
            get { return Resources.IconHunting; }
        }

        public override IconAreas IconArea
        {
            get { return IconAreas.Middle; }
        }

        public override string Label
        {
            get { return "FMH.Hunting".Translate(); }
        }

        public override ManagerJob Selected
        {
            get { return _selected; }
            set { _selected = (ManagerJob_Hunting)value; }
        }

        public void DoContent( Rect rect )
        {
            // layout: settings | animals
            // draw background
            Widgets.DrawMenuSection( rect );
            
            // rects
            var optionsColumnRect = new Rect(
                rect.xMin,
                rect.yMin,
                rect.width * 3 / 5f,
                rect.height - Margin - ButtonSize.y );
            var animalsColumnRect = new Rect(
                optionsColumnRect.xMax,
                rect.yMin,
                rect.width * 2 / 5f,
                rect.height - Margin - ButtonSize.y );
            var buttonRect = new Rect(
                rect.xMax - ButtonSize.x,
                rect.yMax - ButtonSize.y,
                ButtonSize.x - Margin,
                ButtonSize.y - Margin );

            Vector2 position;
            float width;

            // options
            Widgets_Section.BeginSectionColumn( optionsColumnRect, "Hunting.Options", out position, out width );
            Widgets_Section.Section( ref position, width, DrawThresholdSettings, "FM.Threshold".Translate() );
            Widgets_Section.Section( ref position, width, DrawUnforbidCorpses );
            Widgets_Section.Section( ref position, width, DrawHuntingGrounds, "FM.Hunting.AreaRestriction".Translate() );
            Widgets_Section.EndSectionColumn( "Hunting.Options", position );

            // animals
            Widgets_Section.BeginSectionColumn( animalsColumnRect, "Hunting.Animals", out position, out width );
            Widgets_Section.Section( ref position, width, DrawAnimalList, "FMH.Animals".Translate() );
            Widgets_Section.EndSectionColumn( "Hunting.Animals", position );

            // do the button
            if ( !_selected.Managed )
            {
                if ( Widgets.ButtonText( buttonRect, "FM.Manage".Translate() ) )
                {
                    // activate job, add it to the stack
                    _selected.Managed = true;
                    Manager.For( manager ).JobStack.Add( _selected );

                    // refresh source list
                    Refresh();
                }
            }
            else
            {
                if ( Widgets.ButtonText( buttonRect, "FM.Delete".Translate() ) )
                {
                    // inactivate job, remove from the stack.
                    Manager.For( manager ).JobStack.Delete( _selected );

                    // remove content from UI
                    _selected = null;

                    // refresh source list
                    Refresh();
                }
            }
        }

        public float DrawThresholdSettings( Vector2 pos, float width )
        {
            var start = pos; 

            // target count (1)
            int currentCount = _selected.Trigger.CurCount;
            int corpseCount = _selected.GetMeatInCorpses();
            int designatedCount = _selected.GetMeatInDesignations();
            int targetCount = _selected.Trigger.Count;

            _selected.Trigger.DrawTriggerConfig( ref pos, width, ListEntryHeight, false,
                "FMH.TargetCount".Translate(currentCount, corpseCount, designatedCount,
                    targetCount),
                "FMH.TargetCountTooltip".Translate(currentCount, corpseCount,
                    designatedCount, targetCount));

            // allow human & insect meat (2)
            var rowRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
            Utilities.DrawToggle(rowRect, "FMH.AllowHumanMeat".Translate(),
                _selected.Trigger.ThresholdFilter.Allows(Utilities_Hunting.HumanMeat),
                () => _selected.AllowHumanLikeMeat = true,
                () => _selected.AllowHumanLikeMeat = false);
            pos.y += ListEntryHeight;

            rowRect.y += ListEntryHeight;
            Utilities.DrawToggle(rowRect, "FMH.AllowInsectMeat".Translate(),
                _selected.Trigger.ThresholdFilter.Allows(Utilities_Hunting.InsectMeat),
                () => _selected.AllowInsectMeat = true,
                () => _selected.AllowInsectMeat = false);
            pos.y += ListEntryHeight;

            return pos.y - start.y;
        }

        public float DrawUnforbidCorpses( Vector2 pos, float width )
        {
            // unforbid corpses (3)
            var rowRect = new Rect( pos.x, pos.y, width, ListEntryHeight );
            Utilities.DrawToggle(rowRect, "FMH.UnforbidCorpses".Translate(), ref _selected.UnforbidCorpses );
            return ListEntryHeight;
        }

        public float DrawHuntingGrounds( Vector2 pos, float width )
        {
            var rowRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
            AreaAllowedGUI.DoAllowedAreaSelectors(rowRect, ref _selected.HuntingGrounds, manager);
            return ListEntryHeight;
        }

        public float DrawAnimalList( Vector2 pos, float width )
        {
            var start = pos;

            // list of keys in allowed animals list (all animals in biome + visible animals on map)
            var allowedAnimals = _selected.AllowedAnimals;
            var animals = new List<PawnKindDef>( allowedAnimals.Keys );

            // toggle all
            var rowRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
            Utilities.DrawToggle(rowRect, "<i>" + "FM.All".Translate() + "</i>",
                _selected.AllowedAnimals.Values.All(v => v),
                () => animals.ForEach(a => _selected.AllowedAnimals[a] = true),
                () => animals.ForEach(a => _selected.AllowedAnimals[a] = false));

            // toggle predators
            rowRect.y += ListEntryHeight;
            var predators = animals.Where( a => a.RaceProps.predator ).ToList();
            Utilities.DrawToggle(rowRect, "<i>" + "FM.Hunting.Predators".Translate() + "</i>",
                predators.All( p => allowedAnimals[p] ),
                () => predators.ForEach(p => _selected.AllowedAnimals[p] = true),
                () => predators.ForEach(p => _selected.AllowedAnimals[p] = false));

            // toggle herd animals
            rowRect.y += ListEntryHeight;
            var herders = animals.Where( a => a.RaceProps.herdAnimal ).ToList();
            Utilities.DrawToggle( rowRect, "<i>" + "FM.Hunting.HerdAnimals".Translate() + "</i>",
                herders.All( h => allowedAnimals[h] ),
                () => herders.ForEach( h => _selected.AllowedAnimals[h] = true ),
                () => herders.ForEach( h => _selected.AllowedAnimals[h] = false ) );

            // exploding animals
            rowRect.y += ListEntryHeight;
            var exploding = animals
                .Where( a => a.RaceProps.deathActionWorkerClass == typeof( DeathActionWorker_SmallExplosion )
                             || a.RaceProps.deathActionWorkerClass == typeof( DeathActionWorker_BigExplosion ) ).ToList();
            Utilities.DrawToggle(rowRect, "<i>" + "FM.Hunting.Exploding".Translate() + "</i>",
                exploding.All(e => allowedAnimals[e]),
                () => exploding.ForEach(e => _selected.AllowedAnimals[e] = true),
                () => exploding.ForEach(e => _selected.AllowedAnimals[e] = false));

            // toggle for each animal
            foreach (PawnKindDef animal in animals)
            {
                rowRect.y += ListEntryHeight;

                // draw the toggle
                Utilities.DrawToggle(rowRect, animal.LabelCap, _selected.AllowedAnimals[animal],
                    () => _selected.AllowedAnimals[animal] = !_selected.AllowedAnimals[animal]);
            }

            return rowRect.yMax - start.y;
        }

        public void DoLeftRow( Rect rect )
        {
            Widgets.DrawMenuSection( rect );

            // content
            float height = _leftRowHeight;
            var scrollView = new Rect( 0f, 0f, rect.width, height );
            if ( height > rect.height )
                scrollView.width -= ScrollbarWidth;

            Widgets.BeginScrollView( rect, ref _scrollPosition, scrollView );
            Rect scrollContent = scrollView;

            GUI.BeginGroup( scrollContent );
            Vector2 cur = Vector2.zero;
            var i = 0;

            foreach ( ManagerJob_Hunting job in Jobs )
            {
                var row = new Rect( 0f, cur.y, scrollContent.width, LargeListEntryHeight );
                Widgets.DrawHighlightIfMouseover( row );
                if ( _selected == job )
                {
                    Widgets.DrawHighlightSelected( row );
                }

                if ( i++ % 2 == 1 )
                {
                    Widgets.DrawAltRect( row );
                }

                Rect jobRect = row;

                if ( ManagerTab_Overview.DrawOrderButtons( new Rect( row.xMax - 50f, row.yMin, 50f, 50f ), manager, job ) )
                {
                    Refresh();
                }
                jobRect.width -= 50f;

                job.DrawListEntry( jobRect, false, true );
                if ( Widgets.ButtonInvisible( jobRect ) )
                {
                    _selected = job;
                }

                cur.y += LargeListEntryHeight;
            }

            // row for new job.
            var newRect = new Rect( 0f, cur.y, scrollContent.width, LargeListEntryHeight );
            Widgets.DrawHighlightIfMouseover( newRect );

            if ( i++ % 2 == 1 )
            {
                Widgets.DrawAltRect( newRect );
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label( newRect, "<" + "FMH.NewHuntingJob".Translate() + ">" );
            Text.Anchor = TextAnchor.UpperLeft;

            if ( Widgets.ButtonInvisible( newRect ) )
            {
                Selected = new ManagerJob_Hunting( manager );
            }

            TooltipHandler.TipRegion( newRect, "FMH.NewHuntingJobTooltip".Translate() );

            cur.y += LargeListEntryHeight;

            _leftRowHeight = cur.y;
            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        public override void DoWindowContents( Rect canvas )
        {
            // set up rects
            var leftRow = new Rect( 0f, 0f, DefaultLeftRowSize, canvas.height );
            var contentCanvas = new Rect( leftRow.xMax + Margin, 0f, canvas.width - leftRow.width - Margin,
                                          canvas.height );

            // draw overview row
            DoLeftRow( leftRow );

            // draw job interface if something is selected.
            if ( Selected != null )
                DoContent( contentCanvas );
        }

        public override void PreOpen()
        {
            Refresh();
        }

        public void Refresh()
        {
            // upate our list of jobs
            Jobs = Manager.For( manager ).JobStack.FullStack<ManagerJob_Hunting>();

            // update pawnkind options
            foreach ( ManagerJob_Hunting job in Jobs )
                job.UpdateAllowedAnimals();
            _selected?.UpdateAllowedAnimals();
        }
    }
}
