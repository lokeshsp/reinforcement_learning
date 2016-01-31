﻿using core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReinforcementLearning
{
    public partial class ReinforcementLearning : Form
    {
        private GameState _currentState;
        private GameState[] _actionStateArray;
        private QLearningAgent _agent;

        private int _maxUserHp = 5;
        private int _maxTowerHp = 10;
        private int _maxUserPos = 2;

        private int _episodeNumber = 1;
        private int _stepCount;
        private double _totalReward;

        public ReinforcementLearning()
        {
            InitializeComponent();
        }

        private void ReinforcementLearning_Load(object sender, EventArgs e)
        {
            resetModel();
            resetEpisode();
        }

        private void learningTimer_Tick(object sender, EventArgs e)
        {
            renderModel();
        }

        private void renderModel()
        {
            var gContext = BufferedGraphicsManager.Current;
            var buffer = gContext.Allocate(labelRenderingArea.CreateGraphics(), labelRenderingArea.DisplayRectangle);

            buffer.Graphics.Clear(Color.LightSteelBlue);

            GameStateRender.render(buffer.Graphics, _currentState, 20, 20);

            foreach (var elem in _actionStateArray.Select((s, a) => new { State = s, Action = a }))
            {
                if (elem.State == null) continue;

                if (elem.Action == (int)core.Action.LEFT)
                    GameStateRender.render(buffer.Graphics, elem.State, 150, 150);
                else if (elem.Action == (int)core.Action.STOP)
                    GameStateRender.render(buffer.Graphics, elem.State, 250, 250);
                else if (elem.Action == (int)core.Action.RIGHT)
                    GameStateRender.render(buffer.Graphics, elem.State, 350, 350);
            }

            buffer.Render(labelRenderingArea.CreateGraphics());
            buffer.Dispose();
        }

        private void resetModel()
        {
            _agent = new QLearningAgent();

            trackBarAlpha.Value = 2;
            trackBarGamma.Value = 8;
            trackBarEpsilon.Value = 1;

            // regiser state-qvalue
            foreach (var userHp in Enumerable.Range(0, _maxUserHp + 1))
            {
                foreach (var userPos in Enumerable.Range(0, _maxUserPos + 1))
                {
                    foreach (var towerHp in Enumerable.Range(0, _maxTowerHp + 1))
                    {
                        var state = new GameState(userHp, userPos, towerHp);

                        foreach (var action in state.GetActionSet())
                        {
                            _agent.registerStateQValue(state, action, 0);
                        }
                    }
                }
            }
        }

        private void resetEpisode()
        {
            _stepCount = 0;
            _totalReward = 0;
            _currentState = new GameState(_maxUserHp, 1, _maxTowerHp);
            _actionStateArray = new GameState[3];
        }

        private void previewNextSep()
        {
            foreach (var action in _currentState.GetActionSet())
                _actionStateArray[(int)action] = core.Environment.GetNextState(_currentState, action);
        }

        private void selectNextStep()
        {
            var action = _agent.getAction(_currentState);
            foreach (var elem in _actionStateArray.Select((s, a) => new { State = s, Action = a }))
            {
                if ((int)action != elem.Action) _actionStateArray[elem.Action] = null;
            }

            _stepCount++;
            textBoxStepCount.Text = _stepCount.ToString();
        }

        private bool updateNextStep()
        {
            foreach (var elem in _actionStateArray.Select((s, a) => new { State = s, Action = a }))
            {
                if (elem.State != null)
                {
                    var nextS = core.Environment.GetNextState(_currentState, (core.Action)elem.Action);
                    var reward = core.Environment.GetReward(_currentState, (core.Action)elem.Action);
                    _agent.update(_currentState, (core.Action)elem.Action, nextS, reward);
                    _currentState = nextS;

                    _totalReward += reward;
                }
            }

            _actionStateArray[0] = null;
            _actionStateArray[1] = null;
            _actionStateArray[2] = null;

            textBoxTotalReward.Text = _totalReward.ToString();

            if (_currentState.UserHp == 0 || _currentState.TowerHp == 0)
            {
                updateEpisode();
                resetEpisode();
                return false;
            }

            return true;
        }

        private void updateEpisode()
        {
            var row = new string[]
            {
                _episodeNumber.ToString(),
                _stepCount.ToString(),
                (_currentState.UserHp > 0 ? "Win" : "Lose"),
                _totalReward.ToString()
            };
            var item = new ListViewItem(row);
            listViewEpisodeLogs.Items.Add(item);
            listViewEpisodeLogs.Items[listViewEpisodeLogs.Items.Count - 1].EnsureVisible();

            _episodeNumber++;
        }

        private void buttonResetModel_Click(object sender, EventArgs e)
        {
            resetModel();
            resetEpisode();
            renderModel();
        }

        private void buttonPreviewNextStep_Click(object sender, EventArgs e)
        {
            previewNextSep();
        }

        private void buttonSelectNextStep_Click(object sender, EventArgs e)
        {
            selectNextStep();
        }


        private void buttonGotoNextStep_Click(object sender, EventArgs e)
        {
            updateNextStep();
        }

        private void buttonRunEpisode_Click(object sender, EventArgs e)
        {
            new Task(() =>
            {
                foreach (var idx in Enumerable.Range(0, int.Parse(textBoxEpisode.Text)))
                {
                    while (true)
                    {
                        Thread.Sleep(50);
                        previewNextSep();
                        Thread.Sleep(50);
                        selectNextStep();
                        Thread.Sleep(50);
                        var keepGoing = updateNextStep();

                        if (!keepGoing) break;
                    }
                }
            }).Start();
       }

        private void trackBarAlpha_ValueChanged(object sender, EventArgs e)
        {
            _agent.alpha = (double)trackBarAlpha.Value / trackBarAlpha.Maximum;
            labelAlpha.Text = string.Format("alpha {0:0.00}", _agent.alpha);
        }

        private void trackBarGamma_ValueChanged(object sender, EventArgs e)
        {
            _agent.gamma = (double)trackBarGamma.Value / trackBarGamma.Maximum;
            labelGamma.Text = string.Format("gamma {0:0.00}", _agent.gamma);
        }

        private void trackBarEpsilon_ValueChanged(object sender, EventArgs e)
        {
            _agent.epsilon = (double)trackBarEpsilon.Value / trackBarEpsilon.Maximum;
            labelEpsilon.Text = string.Format("epsilon {0:0.00}", _agent.epsilon);
        }

    }
}
