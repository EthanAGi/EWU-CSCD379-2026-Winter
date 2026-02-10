import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AnimalSprite from '../../app/components/AnimalSprite.vue'

describe('AnimalSprite Component', () => {
    it('renders with correct animal data', () => {
        const props = {
            sprite: { id: '1', x: 100, y: 200, vx: 0, vy: 0, bobDelay: 0 },
            animal: { id: '1', name: 'Fluffy', kind: 'cat' },
            isSelected: false
        }
        const component = mount(AnimalSprite, { props })
        expect(component.find('button').exists()).toBe(true)
        expect(component.find('img').exists()).toBe(true)
    })

    it('applies selected class when isSelected is true', () => {
        const props = {
            sprite: { id: '1', x: 100, y: 200, vx: 0, vy: 0, bobDelay: 0 },
            animal: { id: '1', name: 'Fluffy', kind: 'cat' },
            isSelected: true
        }
        const component = mount(AnimalSprite, { props })
        expect(component.find('.selected').exists()).toBe(true)
    })

    it('emits click event when button is clicked', async () => {
        const props = {
            sprite: { id: '1', x: 100, y: 200, vx: 0, vy: 0, bobDelay: 0 },
            animal: { id: '1', name: 'Fluffy', kind: 'cat' },
            isSelected: false
        }
        const component = mount(AnimalSprite, { props })
        await component.find('button').trigger('click')
        expect(component.emitted('click')).toBeTruthy()
        expect(component.emitted('click')?.[0]).toEqual(['1'])
    })

    it('applies facingLeft class when vx is negative', () => {
        const props = {
            sprite: { id: '1', x: 100, y: 200, vx: -5, vy: 0, bobDelay: 0 },
            animal: { id: '1', name: 'Fluffy', kind: 'cat' },
            isSelected: false
        }
        const component = mount(AnimalSprite, { props })
        expect(component.find('.facingLeft').exists()).toBe(true)
    })
})