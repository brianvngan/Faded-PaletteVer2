using System.Collections.Generic;
using UnityEngine;

public class TurnBasedCardBattle : MonoBehaviour
{
    public enum CardType
    {
        Attack,
        Defend,
        Escape,
        Heal
    }

    [Header("Battle Setup")]
    [SerializeField] private int playerMaxHp = 5;
    [SerializeField] private int enemyMaxHp = 5;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private Transform playerSprite;
    [SerializeField] private Transform enemySprite;
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Card Draw")]
    [SerializeField] private int visibleCardsPerTurn = 2;

    private int playerHp;
    private int enemyHp;
    private bool battleActive;
    private bool playerTurn;
    private bool playerShield;
    private bool enemyShield;
    private Vector3 playerStartPosition;
    private Vector3 enemyStartPosition;
    private readonly List<CardType> playerCards = new List<CardType>();
    private readonly List<CardType> enemyCards = new List<CardType>();
    private readonly System.Random random = new System.Random();
    private string battleMessage = "";

    private void Awake()
    {
        playerHp = playerMaxHp;
        enemyHp = enemyMaxHp;

        if (playerSprite == null)
        {
            playerSprite = transform;
        }

        playerStartPosition = playerSprite.position;

        if (enemySprite != null)
        {
            enemyStartPosition = enemySprite.position;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (battleActive)
        {
            return;
        }

        if (other.CompareTag(enemyTag))
        {
            StartBattle(other.transform);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (battleActive)
        {
            return;
        }

        if (collision.collider.CompareTag(enemyTag))
        {
            StartBattle(collision.collider.transform);
        }
    }

    private void StartBattle(Transform enemyTransform)
    {
        enemySprite = enemyTransform;
        enemyStartPosition = enemySprite.position;

        battleActive = true;
        playerTurn = true;
        playerShield = false;
        enemyShield = false;
        battleMessage = "Choose a card.";

        DrawPlayerCards();
        DrawEnemyCards();
    }

    private void DrawPlayerCards()
    {
        playerCards.Clear();
        FillHand(playerCards, new[] { CardType.Attack, CardType.Defend, CardType.Escape }, visibleCardsPerTurn);
    }

    private void DrawEnemyCards()
    {
        enemyCards.Clear();
        FillHand(enemyCards, new[] { CardType.Attack, CardType.Defend, CardType.Heal }, visibleCardsPerTurn);
    }

    private void FillHand(List<CardType> hand, CardType[] deck, int count)
    {
        for (int i = 0; i < count; i++)
        {
            hand.Add(deck[random.Next(deck.Length)]);
        }
    }

    private void OnGUI()
    {
        if (!battleActive)
        {
            return;
        }

        float panelWidth = 520f;
        float panelHeight = 170f;
        Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f, Screen.height - panelHeight - 20f, panelWidth, panelHeight);

        GUI.Box(panelRect, "Battle");

        Rect statusRect = new Rect(panelRect.x + 16f, panelRect.y + 28f, panelRect.width - 32f, 56f);
        GUI.Label(statusRect, $"Player HP: {playerHp}/{playerMaxHp}    Enemy HP: {enemyHp}/{enemyMaxHp}\n{battleMessage}");

        if (playerTurn)
        {
            DrawCardButton(panelRect, 0, playerCards.Count > 0 ? playerCards[0] : CardType.Attack, UsePlayerCard);
            DrawCardButton(panelRect, 1, playerCards.Count > 1 ? playerCards[1] : CardType.Attack, UsePlayerCard);
        }
        else
        {
            GUI.Label(new Rect(panelRect.x + 16f, panelRect.y + 106f, panelRect.width - 32f, 24f), "Enemy is choosing a card...");
        }
    }

    private void DrawCardButton(Rect panelRect, int index, CardType card, System.Action<int> onClick)
    {
        float buttonWidth = 145f;
        float buttonHeight = 44f;
        float gap = 20f;
        float totalWidth = (buttonWidth * 2f) + gap;
        float startX = panelRect.x + (panelRect.width - totalWidth) * 0.5f;
        float y = panelRect.y + 104f;

        if (GUI.Button(new Rect(startX + index * (buttonWidth + gap), y, buttonWidth, buttonHeight), FormatCardName(card)))
        {
            onClick(index);
        }
    }

    private void UsePlayerCard(int index)
    {
        if (!playerTurn || index < 0 || index >= playerCards.Count)
        {
            return;
        }

        ResolveCard(playerCards[index], isPlayerCard: true);

        if (CheckBattleEnd())
        {
            return;
        }

        playerTurn = false;
        battleMessage = "Enemy turn.";
        Invoke(nameof(ResolveEnemyTurn), 0.7f);
    }

    private void ResolveEnemyTurn()
    {
        if (!battleActive)
        {
            return;
        }

        DrawEnemyCards();

        int chosenIndex = random.Next(enemyCards.Count);
        ResolveCard(enemyCards[chosenIndex], isPlayerCard: false);

        if (CheckBattleEnd())
        {
            return;
        }

        playerTurn = true;
        DrawPlayerCards();
        battleMessage = "Choose a card.";
    }

    private void ResolveCard(CardType card, bool isPlayerCard)
    {
        switch (card)
        {
            case CardType.Attack:
                if (isPlayerCard)
                {
                    DealDamageToEnemy(1);
                    battleMessage = "You attacked for 1 damage.";
                }
                else
                {
                    DealDamageToPlayer(1);
                    battleMessage = "Enemy attacked for 1 damage.";
                }
                break;
            case CardType.Defend:
                if (isPlayerCard)
                {
                    playerShield = true;
                    battleMessage = "You are defending.";
                }
                else
                {
                    enemyShield = true;
                    battleMessage = "Enemy is defending.";
                }
                break;
            case CardType.Escape:
                if (isPlayerCard)
                {
                    EscapeBattle();
                    battleMessage = "You escaped.";
                }
                break;
            case CardType.Heal:
                if (!isPlayerCard)
                {
                    enemyHp = Mathf.Min(enemyHp + 1, enemyMaxHp);
                    battleMessage = "Enemy healed 1 HP.";
                }
                break;
        }
    }

    private void DealDamageToEnemy(int amount)
    {
        if (enemyShield)
        {
            enemyShield = false;
            return;
        }

        enemyHp = Mathf.Max(enemyHp - amount, 0);
    }

    private void DealDamageToPlayer(int amount)
    {
        if (playerShield)
        {
            playerShield = false;
            return;
        }

        playerHp = Mathf.Max(playerHp - amount, 0);
    }

    private void EscapeBattle()
    {
        battleActive = false;
        playerTurn = false;
        playerShield = false;
        enemyShield = false;
        playerCards.Clear();
        enemyCards.Clear();

        if (playerSprite != null)
        {
            Vector3 retreatDirection = (playerSprite.position - enemyStartPosition).normalized;
            if (retreatDirection == Vector3.zero)
            {
                retreatDirection = Vector3.left;
            }

            playerSprite.position += retreatDirection * tileSize * 2f;
        }
    }

    private bool CheckBattleEnd()
    {
        if (playerHp <= 0)
        {
            battleActive = false;
            battleMessage = "You were defeated.";
            return true;
        }

        if (enemyHp <= 0)
        {
            battleActive = false;
            battleMessage = "Enemy defeated.";
            return true;
        }

        return false;
    }

    private static string FormatCardName(CardType card)
    {
        return card switch
        {
            CardType.Attack => "Attack",
            CardType.Defend => "Defend",
            CardType.Escape => "Escape",
            CardType.Heal => "Heal",
            _ => card.ToString()
        };
    }

    public void ResetBattle()
    {
        playerHp = playerMaxHp;
        enemyHp = enemyMaxHp;
        battleActive = false;
        playerTurn = false;
        playerShield = false;
        enemyShield = false;
        battleMessage = "";
        playerCards.Clear();
        enemyCards.Clear();

        if (playerSprite != null)
        {
            playerSprite.position = playerStartPosition;
        }

        if (enemySprite != null)
        {
            enemySprite.position = enemyStartPosition;
        }
    }
}