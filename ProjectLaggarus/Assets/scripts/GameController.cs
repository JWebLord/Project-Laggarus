using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour {

    /// <summary>
    /// Массив игроков
    /// </summary>
    public PlayerController[] Players;
    private int currentPlayerNum;
    public HexGameUI GameUI;

    /// <summary>
    /// Ссылка на грид этого игрока
    /// </summary>
    public HexGrid grid;

    /// <summary>
    /// Ссылка на текстовое поле для вывода текущего хода
    /// </summary>
    public Text currentTurnLabel;

    /// <summary>
    /// Ссылка на текстовое поле для вывода текущего игрока
    /// </summary>
    public Text currentPlayerLabel;

    //TODO сделать динамическое количество игроков

    /// <summary>
    /// Константа количества игроков
    /// </summary>
    private const int playersCount = 2;

    public int CurrentPlayerNum
    {
        get
        {
            return currentPlayerNum;
        }
    }

    private void Awake()
    {
        Players = new PlayerController[playersCount];

        for(int i = 0; i < playersCount; i++)
        {
            Players[i] = new PlayerController();
        }

        currentPlayerNum = 0;
    }

    /// <summary>
    /// Поменять текущего игрока
    /// </summary>
    public void ChangeCurrentPlayer()
    {
        if (currentPlayerNum < Players.Length - 1)
        {
            currentPlayerNum++;
        }
        else
        {
            currentPlayerNum = 0;
        }
    }

    /// <summary>
    /// Завершить ход(запуск процедур обсчета и всего, что необходимо)
    /// </summary>
    public void DoPlayerTurn()
    {
        Players[currentPlayerNum].currentTurn++;
        Players[currentPlayerNum].DoTurn(grid);
        GameUI.CancelSelection();//снимаем выделение с юнитов

        ChangeCurrentPlayer();
        currentTurnLabel.text = "Current Turn: " + Players[currentPlayerNum].currentTurn.ToString();
        currentPlayerLabel.text = "Current Player: " + currentPlayerNum.ToString();
    }
}
