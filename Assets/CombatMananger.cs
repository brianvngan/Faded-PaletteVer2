using UnityEngine;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    public enum CardType
    {
        Attack,
        Defend,
        Heal
    }

    [Header("Player Stats")]
    public int playerMaxHP = 10;
    public int playerCurrentHP = 10;
    public int playerAttackDamage = 2;
    public int playerHealAmount = 3;

    [Header("References")]
    public Transform player;
    public GridSpawner gridSpawner;
    
    [Header("Card Settings")]
    public int handSize = 2;

    [Header("State")]
    public bool playerDefending = false;
    public bool playerFrozenForOneTurn = false;
    public bool playerDead = false;
    public bool floorCleared = false;

    private List<CardType> playerHand = new List<CardType>();
    private string lastActionMessage = "";
    private float messageDisplayTime = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        for (int i = 0; i < handSize; i++)
            playerHand.Add(GetRandomCard());
    }

    CardType GetRandomCard()
    {
        int roll = Random.Range(0, 3);
        if (roll == 0) return CardType.Attack;
        if (roll == 1) return CardType.Defend;
        return CardType.Heal;
    }

    public void PlayCard(int handIndex)
    {
        if (playerDead || floorCleared) return;
        if (handIndex < 0 || handIndex >= playerHand.Count) return;

        CardType card = playerHand[handIndex];
        bool cardUsed = false;

        switch (card)
        {
            case CardType.Attack:
                cardUsed = TryAttackAdjacentEnemy();
                break;
            case CardType.Defend:
                playerDefending = true;
                ShowMessage("Defending! Will block next attack.");
                cardUsed = true;
                break;
            case CardType.Heal:
                if (playerCurrentHP >= playerMaxHP)
                {
                    ShowMessage("Already at full HP!");
                    cardUsed = false;
                }
                else
                {
                    int healed = Mathf.Min(playerHealAmount, playerMaxHP - playerCurrentHP);
                    playerCurrentHP += healed;
                    ShowMessage($"Healed {healed} HP!");
                    cardUsed = true;
                }
                break;
        }

        if (cardUsed)
        {
            playerHand[handIndex] = GetRandomCard();
            Invoke(nameof(EnemyCounterAttack), 0.5f);
        }
    }

    void EnemyCounterAttack()
    {
        if (EnemyManager.Instance == null) return;
        
        List<EnemyAI> adjacentEnemies = EnemyManager.Instance.GetAllAdjacentEnemies(player.position);
        
        foreach (EnemyAI enemy in adjacentEnemies)
        {
            if (enemy == null) continue;
            TakeDamageFromEnemy(enemy.attackDamage);
        }
    }

    bool TryAttackAdjacentEnemy()
    {
        if (EnemyManager.Instance == null) return false;

        EnemyAI target = EnemyManager.Instance.GetAdjacentEnemy(player.position);
        
        if (target == null)
        {
            ShowMessage("No enemy in range!");
            return false;
        }

        target.TakeDamage(playerAttackDamage);
        ShowMessage($"Attacked for {playerAttackDamage} damage!");

        if (target.currentHP <= 0)
        {
            ShowMessage($"Defeated enemy!");
            Destroy(target.gameObject);
        }

        return true;
    }

    public void TakeDamageFromEnemy(int amount)
    {
        if (playerDead || floorCleared) return;

        if (playerDefending)
        {
            amount = Mathf.Max(0, amount - 1);
            playerDefending = false;
            ShowMessage($"Blocked! Took {amount} damage.");
        }
        else
        {
            ShowMessage($"Took {amount} damage!");
        }

        playerCurrentHP = Mathf.Max(0, playerCurrentHP - amount);
        playerFrozenForOneTurn = true;

        if (playerCurrentHP <= 0)
        {
            ShowMessage("You died!");
            playerDead = true;
        }
    }

    void ShowMessage(string msg)
    {
        lastActionMessage = msg;
        messageDisplayTime = Time.time + 2f;
        Debug.Log(msg);
    }

    void Update()
    {
        if (Time.time > messageDisplayTime)
            lastActionMessage = "";

        // Check if player reached the clear tile
        if (!floorCleared && !playerDead && gridSpawner != null && player != null)
        {
            if (gridSpawner.IsPlayerOnClearTile(player.position))
            {
                floorCleared = true;
                ShowMessage("Floor Cleared!");
                Debug.Log("🎉 Floor Cleared!");
            }
        }
    }

    void OnGUI()
    {
        GUIStyle hpStyle = new GUIStyle(GUI.skin.box) 
        { 
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter
        };
        
        GUIStyle messageStyle = new GUIStyle(GUI.skin.box) 
        { 
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };
        
        GUIStyle cardStyle = new GUIStyle(GUI.skin.button) 
        { 
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) 
        { 
            fontSize = 16
        };

        GUI.Box(new Rect(10, 10, 250, 40), $"HP: {playerCurrentHP}/{playerMaxHP}", hpStyle);

        float yOffset = 60;
        if (playerDefending)
        {
            GUI.Label(new Rect(10, yOffset, 250, 25), "Defending", labelStyle);
            yOffset += 30;
        }
        if (playerFrozenForOneTurn)
        {
            GUI.Label(new Rect(10, yOffset, 250, 25), "Frozen (1 turn)", labelStyle);
        }

        if (!string.IsNullOrEmpty(lastActionMessage))
        {
            GUI.Box(new Rect(Screen.width / 2 - 250, 50, 500, 40), lastActionMessage, messageStyle);
        }

        // Only show cards if not dead and not cleared
        if (!playerDead && !floorCleared)
        {
            DrawCards(cardStyle);
        }

        // Death screen
        if (playerDead)
        {
            DrawEndScreen("You Died", Color.red, "Retry");
        }

        // Floor cleared screen
        if (floorCleared)
        {
            DrawEndScreen("Floor Cleared!", Color.yellow, "Next Floor");
        }
    }

    void DrawEndScreen(string title, Color titleColor, string buttonText)
    {
        // Black overlay
        GUI.color = new Color(0, 0, 0, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Title text
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 80,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = titleColor },
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(0, Screen.height / 2 - 100, Screen.width, 100), title, titleStyle);

        // Button
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 30,
            alignment = TextAnchor.MiddleCenter
        };
        if (GUI.Button(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 20, 300, 60), buttonText, buttonStyle))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }

    void DrawCards(GUIStyle cardStyle)
    {
        float cardWidth = 150;
        float cardHeight = 200;
        float spacing = 15;
        float totalWidth = (cardWidth * handSize) + (spacing * (handSize - 1));
        float startX = Screen.width - totalWidth - 20;
        float startY = Screen.height - cardHeight - 20;

        for (int i = 0; i < playerHand.Count; i++)
        {
            float x = startX + i * (cardWidth + spacing);
            Rect cardRect = new Rect(x, startY, cardWidth, cardHeight);

            bool canPlayCard = CanPlayCard(playerHand[i]);
            GUI.enabled = canPlayCard;
            
            if (GUI.Button(cardRect, GetCardLabel(playerHand[i]), cardStyle))
            {
                PlayCard(i);
            }
            
            GUI.enabled = true;
        }
    }

    bool CanPlayCard(CardType card)
    {
        switch (card)
        {
            case CardType.Attack:
                if (EnemyManager.Instance == null) return false;
                return EnemyManager.Instance.GetAdjacentEnemy(player.position) != null;
            
            case CardType.Defend:
                return true;
            
            case CardType.Heal:
                return playerCurrentHP < playerMaxHP;
        }
        return false;
    }

    string GetCardLabel(CardType card)
    {
        switch (card)
        {
            case CardType.Attack: return "⚔️\n\nATTACK\n\nDamage\nadjacent enemy";
            case CardType.Defend: return "🛡️\n\nDEFEND\n\nBlock next\nincoming attack";
            case CardType.Heal: return "💚\n\nHEAL\n\nRestore HP";
        }
        return "";
    }
}