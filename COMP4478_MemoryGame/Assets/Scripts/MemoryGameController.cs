using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MemoryGameController : MonoBehaviour
{
    //Public variables
    [Range(2,16)] public int cardsToSpawn = 16; //Number of cards to spawn
    public List<Button> cardButtons = new List<Button>(); //list of buttons on cards
    public float cardCheckDelay = 1f, cardUnflipDelay = 0.75f; //Delays for (un)flipping
    public AudioSource flipSound, unflipSound, shuffleSound; //SFx
    
    [Header("Game Over Stuff")] public GameObject gameOverScreen;
    public TMPro.TMP_Text gameOverGuessesText, matchesToSpawnText;
    public Slider matchesToSpawnSlider;
    
    //Private variables
    [SerializeField] private Transform gridTransform; //Game grid transform
    [SerializeField] private GameObject[] cardPrefabs;

    private bool guess1, guess2; //Each card flipped counts as guess 1 and 2 
    private int guess1Index, guess2Index; //Index of guessed card in cardButtons list
    private int guess1Variant, guess2Variant; //Variant of card guessed
    private int guessCount, correctCount, totalMatches; //How many guesses | correct guesses | total matches available
    private static readonly int Flipped = Animator.StringToHash("flipped");

    //Methods
    void Awake()
    {
        //Initialize methods
        SpawnCards();
        
        //Set variables
        gameOverScreen.SetActive(false);
        matchesToSpawnSlider.value = cardsToSpawn / 2;
        matchesToSpawnText.text = "New Number of Matches: " + (int) matchesToSpawnSlider.value;
    }
    
    void SpawnCards()
    {
        for (int i = 0; i < cardsToSpawn/2; i++) //Spawn the cards
        {
            int variant = UnityEngine.Random.Range(0, cardPrefabs.Length - 1); //Choose a random variant
            for (int j = 0; j < 2; j++) //Instantiate 2 of same card type
            {
                GameObject card = Instantiate(cardPrefabs[variant]);
                card.name += "_" + (i + j);
                card.transform.SetParent(gridTransform, false);
            }
        }
        GetButtons();
        AddListeners();
        ShuffleCards(cardButtons);
        totalMatches = cardButtons.Count / 2; //Calculate how many matches are in the game
    }
    
    void GetButtons() //Get card cardButtons and store in list
    {
        GameObject[] cards = GameObject.FindGameObjectsWithTag("Card");
        for (int i = 0; i < cards.Length; i++)
        {
            cardButtons.Add(cards[i].GetComponent<Button>());
        }
    }
    
    void AddListeners() //Add event listeners to card cardButtons
    {
        foreach (var btn in cardButtons)
            btn.onClick.AddListener(() => FlipCard()); //When card clicked, call FlipCard()
    }
    
    //Shuffles the list index positions of the cards, which the canvas grid automatically organizes
    void ShuffleCards(List<Button> list) 
    {
        for (int i = 0; i < list.Count; i++) //Loop through the list
        {
            int randomIndex = UnityEngine.Random.Range(0, list.Count);
            list[i].transform.SetSiblingIndex(randomIndex); //Set random sibling index in hierarchy
        }
    }
    
    public void FlipCard() //Flips a card if player has guesses and sets guess variables
    {
        var selectedCard = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
        var cardAnimator = selectedCard.GetComponent<Animator>();
        var cardVariant = -1;
        var cardIndex = -1;
        
        if (selectedCard.name.Contains("variant_")) //If card is valid
        {
            var str = selectedCard.name.Split('_'); //Split card name
            int.TryParse(str[2], out cardVariant); //Get Card variant from name
            //Get card index in cardButtons list
            foreach (var btn in cardButtons)
                if (btn.gameObject == selectedCard)
                    cardIndex = cardButtons.IndexOf(btn);
        }
    
        if (!guess2)
        {
            //If 2 guesses haven't been used up yet
            if (!guess1)
            {
                guess1 = true;
                guess1Index = cardIndex;
                guess1Variant = cardVariant;
                cardButtons[guess1Index].enabled = false; //Disable button
                //Debug.Log("guess1Index: " + guess1Index);
    
            }
            else if (!guess2 && cardIndex != guess1Index)
            {
                guess2 = true;
                guess2Index = cardIndex;
                guess2Variant = cardVariant;
                cardButtons[guess2Index].enabled = false; //Disable button
                //Debug.Log("guess2Index: " + guess2Index);
    
                StartCoroutine(CheckForMatch()); //Check if guesses match
                guessCount++; //Increment number of guesses taken
            }
            
            //Play card flip animation
            cardAnimator.SetTrigger(Flipped);
            //Play flip sound
            if(flipSound) flipSound.PlayOneShot(flipSound.clip);
        }
    }
    
    IEnumerator CheckForMatch() //Checks if guesses are a match
    {
        yield return new WaitForSeconds(cardCheckDelay); //Wait till cards flipped
    
        if (guess1Variant == guess2Variant) //Match
        {
            correctCount++;
            if (correctCount == totalMatches) //If all matches made, end game
                GameOver();
        }
        else //No match
        {
            //Re-enable cardButtons
            cardButtons[guess1Index].enabled = true; cardButtons[guess2Index].enabled = true;
            //Flip cards back over
            var card1Animator = cardButtons[guess1Index].GetComponent<Animator>();
            var card2Animator = cardButtons[guess2Index].GetComponent<Animator>();
            card1Animator.ResetTrigger(Flipped); card2Animator.ResetTrigger(Flipped);
            //Play flip sound twice (to sound like flipping 2 cards)
            if (unflipSound)
            {
                unflipSound.PlayOneShot(unflipSound.clip);
                unflipSound.PlayOneShot(unflipSound.clip);
            }
            yield return new WaitForSeconds(cardUnflipDelay);
        }
        guess1 = guess2 = false; //Reset guesses
    }
    
    void GameOver() //Ends the game
    {
        //Debug.Log("Game Over");
        gameOverScreen.SetActive(true); //Enable game over screen
        gameOverGuessesText.text = "Guesses: " + guessCount;
    }
    
    public void RestartGame() //Restarts the game
    {
        gameOverScreen.SetActive(false); //Disable game over screen
        StartCoroutine(RespawnCards()); //Reshuffle cards
    }
    
    IEnumerator RespawnCards() //Respawns all the cards
    {
        foreach (var btn in cardButtons) //Unflip all cards
        {
            var animator = btn.GetComponent<Animator>();
            animator.ResetTrigger(Flipped);
            if (unflipSound) unflipSound.PlayOneShot(unflipSound.clip);
        }
        
        yield return new WaitForSeconds(cardUnflipDelay); //Wait for unflip
        
        //Delete the cards
        foreach (var btn in cardButtons) 
            Destroy(btn.gameObject);
    
        //Reset variables
        guessCount = 0;
        guess1 = guess2 = false;
        guess1Index = guess2Index = guess1Variant = guess2Variant = 0;
        correctCount = 0;
        cardButtons.Clear();
    
        yield return new WaitForEndOfFrame(); //Wait for end of frame so all references are cleared
        
        //Spawn new cards
        SpawnCards();
    }
    
    public void SetNumCardsToSpawn()
    {
        cardsToSpawn = (int) matchesToSpawnSlider.value * 2; //Set number of cards to spawn
        matchesToSpawnText.text = "New Number of Matches: " + (int) matchesToSpawnSlider.value;
    }

}
