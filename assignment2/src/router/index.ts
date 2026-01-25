import { createRouter, createWebHistory } from "vue-router";
import HomeView from "../views/HomeView.vue";
import GameView from "../views/game.vue";
import SecretView from "../views/Secret.vue"; // ✅ match your actual filename

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: "/", name: "home", component: HomeView },
    { path: "/game", name: "game", component: GameView },
    { path: "/secret", name: "secret", component: SecretView }, // ✅ add route
  ],
});

export default router;
