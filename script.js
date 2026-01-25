const cards = document.querySelectorAll(".section-card");
const currentMonth = new Date().getMonth() + 1;

cards.forEach((card, index) => {
  const month = Number(card.dataset.month);
  const delay = index * 0.08;
  card.style.animationDelay = `${delay}s`;

  if (month > currentMonth) {
    card.classList.add("locked");
  }
});
